# AI

## Model
<!-- ::xmind-pos:{"x":128,"y":-7} -->

### Purpose

> Owns  
> Analysis  
> Logical deduction  
> Explanation  
> Synthesis  
> Pattern recognition  
>   
### Responsibilities

- Reasoning Capability
  
  > This is probably the most misunderstood responsibility.  
  >   
  > Reasoning capability is the Model’s ability to analyze information presented to it and produce a coherent conclusion.  
  >   
  > Examples include:  
  > Comparing alternatives  
  > Finding inconsistencies  
  > Explaining concepts  
  > Following logical relationships  
  > Breaking problems into steps  
  > Applying knowledge to new situations  
  >   
  > Notice something important:  
  > The Model is reasoning about the information it has been given.  
  >   
  > It is not deciding what information to gather.  
  >   
  > It is not deciding whether another tool should be called.  
  >   
- Inference
  
  > Inference is the actual execution of the trained model.  
  >   
  > When someone says:  
  > “Run the model.”  
  >   
  > they’re talking about inference.  
  >   
  > Everything the Model produces comes from inference.  
  >   
  > Examples:  
  > Completing text  
  > Answering questions  
  > Summarizing documents  
  > Classifying text  
  > Creating embeddings  
  > Translating languages  
  >   
  > Inference is the act of using the trained model.  
  >   
  > Training created the intelligence.  
  >   
  > Inference applies it.  
  >   
- Generation
  
  > Generation is producing new content.  
  > That content could be:  
  > Text  
  > Source code  
  > JSON  
  > SQL  
  > HTML  
  > Markdown  
  > Images  
  > Audio  
  >   
  > Generation answers:  
  > “Given this input, produce something new.”  
  >   
  > Notice that generation doesn’t imply correctness.  
  >   
  > A model can generate:  
  > an excellent explanation  
  > poor code  
  > a beautiful poem  
  > an incorrect conclusion  
  >   
  > Generation simply means creating output.  
  >   
  >   
- Transformation
  
  > Transformation is different from generation.  
  > Instead of creating something entirely new, transformation changes one representation into another.  
  >   
  > Examples:  
  >   
  > English → French  
  > Messy notes → Summary  
  > Markdown → HTML  
  > Large document → Bullet list  
  > JSON → C# classes  
  > Voice → Text  
  >   
  > Transformation preserves the underlying meaning while changing the representation.  
  > That’s an important distinction.  
  >   
- Processing the resulting token sequence
  
  > Responds to prompts  
  >   
### Exclusions

- workflow
  
- tool execution
  
- application state
  
- security policy
  
- retrieval
  
- prompt construction
  
### Characteristics

- Context Window
  
- Tokenizer
  
- Supported Modalities
  <!-- ::xmind-pos:{"x":391,"y":5} -->
  
- Parameter Count
  
- Latency
  
- Cost
  
### Inputs

- Token Sequence
  
### Outputs

- Generated Tokens
  
### Contracts

## Application

### Exclusions

- Business rules
  
- Configuration
  
- Application state
  
- Response rendering
  
- Session management
  
- Workflow
  
- Tool execution
  
- Retrieval
  
- Memory persistence
  
### Possible Responsibilities

- External Interaction
  
  > External Interaction  
  > Owns communication between the AI system and external actors or systems. The interaction may originate from a human user, another application, an operating system event, a scheduled task, or a messaging system.  
  >   
  > Examples:  
  >  Web application (HTTP/HTML)   
  >  REST API (HTTP/JSON)   
  >  CLI (stdin/stdout)   
  >  Queue consumer (messages/events)   
  >  Windows service (timers, OS events, queues)  
  >   
  >   
- Initiates authentication flow
  
- Passes authentication evidence across boundaries
  
- Prompt construction
  
## Security / Identity

### Possible Responsibilities

- Authentication
  
  > Who or what is this actor?  
  >   
- Authorization
  
  > What is this actor allowed to do?  
  >   
- Credential handling
  
- Token validation
  
- Claims / identity evidence
  
- Policy enforcement
  
## Agent

### Possible Responsibilities

- Deciding what needs to be asked next
  
## Tool

## Retrieval

## Memory

