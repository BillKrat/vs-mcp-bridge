USE [db_a2cb58_adventure_1]
GO

/****** Object:  StoredProcedure [dbo].[UpsertBlogPostFromRepo]    Script Date: 4/18/2026 12:59:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[UpsertBlogPostFromRepo]
    @BlogID UNIQUEIDENTIFIER,
    @PostID UNIQUEIDENTIFIER,
    @Title NVARCHAR(255),
    @Description NVARCHAR(MAX),
    @PostContent NVARCHAR(MAX),
    @Author NVARCHAR(50),
    @Slug NVARCHAR(255),
    @IsPublished BIT = 0,
    @IsCommentEnabled BIT = 1,
    @Categories dbo.StringList READONLY,
    @Tags dbo.StringList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RequestedCategories TABLE
    (
        CategoryName NVARCHAR(255) NOT NULL,
        NormalizedCategoryName NVARCHAR(255) NOT NULL PRIMARY KEY
    );

    DECLARE @ResolvedCategories TABLE
    (
        CategoryID UNIQUEIDENTIFIER NOT NULL,
        CategoryName NVARCHAR(255) NOT NULL,
        NormalizedCategoryName NVARCHAR(255) NOT NULL PRIMARY KEY
    );

    DECLARE @NormalizedTags TABLE
    (
        Tag NVARCHAR(255) NOT NULL,
        NormalizedTag NVARCHAR(255) NOT NULL PRIMARY KEY
    );

    INSERT INTO @RequestedCategories
    (
        CategoryName,
        NormalizedCategoryName
    )
    SELECT
        MIN(TrimmedValue) AS CategoryName,
        LOWER(TrimmedValue) AS NormalizedCategoryName
    FROM
    (
        SELECT LTRIM(RTRIM(src.Value)) AS TrimmedValue
        FROM @Categories src
    ) normalized
    WHERE TrimmedValue <> N''
    GROUP BY LOWER(TrimmedValue);

    INSERT INTO @ResolvedCategories
    (
        CategoryID,
        CategoryName,
        NormalizedCategoryName
    )
    SELECT
        c.CategoryID,
        c.CategoryName,
        LOWER(LTRIM(RTRIM(c.CategoryName))) AS NormalizedCategoryName
    FROM dbo.be_Categories c
    INNER JOIN @RequestedCategories requested
        ON LOWER(LTRIM(RTRIM(c.CategoryName))) = requested.NormalizedCategoryName
    WHERE c.BlogID = @BlogID;

    IF EXISTS
    (
        SELECT 1
        FROM @RequestedCategories requested
        LEFT JOIN @ResolvedCategories resolved
            ON resolved.NormalizedCategoryName = requested.NormalizedCategoryName
        WHERE resolved.NormalizedCategoryName IS NULL
    )
    BEGIN
        DECLARE @MissingCategories NVARCHAR(MAX);
        DECLARE @CategoryErrorMessage NVARCHAR(MAX);

        SELECT @MissingCategories =
            STRING_AGG(requested.CategoryName, N', ')
        FROM @RequestedCategories requested
        LEFT JOIN @ResolvedCategories resolved
            ON resolved.NormalizedCategoryName = requested.NormalizedCategoryName
        WHERE resolved.NormalizedCategoryName IS NULL;

        SET @CategoryErrorMessage =
            N'UpsertBlogPostFromRepo failed: missing categories for BlogID '
            + CONVERT(NVARCHAR(36), @BlogID)
            + N': '
            + ISNULL(@MissingCategories, N'<unknown>');

        THROW 50000, @CategoryErrorMessage, 1;
    END

    INSERT INTO @NormalizedTags
    (
        Tag,
        NormalizedTag
    )
    SELECT
        MIN(TrimmedValue) AS Tag,
        LOWER(TrimmedValue) AS NormalizedTag
    FROM
    (
        SELECT LTRIM(RTRIM(src.Value)) AS TrimmedValue
        FROM @Tags src
    ) normalized
    WHERE TrimmedValue <> N''
    GROUP BY LOWER(TrimmedValue);

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Upsert post
        IF EXISTS (
            SELECT 1
            FROM dbo.be_Posts
            WHERE BlogID = @BlogID
              AND PostID = @PostID
        )
        BEGIN
            UPDATE dbo.be_Posts
            SET
                Title = @Title,
                Description = @Description,
                PostContent = @PostContent,
                DateModified = GETDATE(),
                Author = @Author,
                IsPublished = @IsPublished,
                IsCommentEnabled = @IsCommentEnabled,
                Slug = @Slug,
                IsDeleted = 0
            WHERE BlogID = @BlogID
              AND PostID = @PostID;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.be_Posts
            (
                BlogID,
                PostID,
                Title,
                Description,
                PostContent,
                DateCreated,
                DateModified,
                Author,
                IsPublished,
                IsCommentEnabled,
                Raters,
                Rating,
                Slug,
                IsDeleted
            )
            VALUES
            (
                @BlogID,
                @PostID,
                @Title,
                @Description,
                @PostContent,
                GETDATE(),
                GETDATE(),
                @Author,
                @IsPublished,
                @IsCommentEnabled,
                0,
                0,
                @Slug,
                0
            );
        END

        -- Replace categories
        DELETE FROM dbo.be_PostCategory
        WHERE BlogID = @BlogID
          AND PostID = @PostID;

        INSERT INTO dbo.be_PostCategory
        (
            BlogID,
            PostID,
            CategoryID
        )
        SELECT
            @BlogID,
            @PostID,
            resolved.CategoryID
        FROM @ResolvedCategories resolved;

        -- Replace tags
        DELETE FROM dbo.be_PostTag
        WHERE BlogID = @BlogID
          AND PostID = @PostID;

        INSERT INTO dbo.be_PostTag
        (
            BlogID,
            PostID,
            Tag
        )
        SELECT
            @BlogID,
            @PostID,
            normalized.Tag
        FROM @NormalizedTags normalized;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO


