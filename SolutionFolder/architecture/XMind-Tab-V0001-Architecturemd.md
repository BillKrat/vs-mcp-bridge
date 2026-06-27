# Architecture

> The framework exists to discover stable responsibility boundaries, not to preserve our first ideas.  
>   
## Discovery

### Does this belong in framework or notes?

- Framework
  
    - Things that survive repeated investigation
      
- Notes
  
    - Everything else
      
## Domain Template
<!-- ::xmind-pos:{"x":128,"y":-7} -->

> Child nodes optional  
>   
### Purpose

### Responsibilities

> What the domain is accountable for owning.  
>   
### Exclusions

### Characteristics

### Inputs

### Outputs

### Contracts

### Possible Responsibilites

> Disappears once the domain is mature  
>   
## Architectural Heuristics

> How should we think while investigating?  
>   
### Purpose emerges from responsibilities.

### Responsibilities define ownership.

### Strongest claim wins.

### Test across multiple implementations.

### Move hypotheses freely.

### Discovery before refinement.

### Refinement before publication.

### Boundaries before contracts.

### Contracts before interfaces.

### Emerging Concepts

- Mechanism (candidate)
  
### A responsibility should survive multiple implementations of the same kind of system.

> Example  
> Web App  
> REST Service  
> Windows Service  
> CLI  
> Queue Consumer  
>   
## Qualitifaction Tests

> Should this become a responsibility domain?  
>   
### Domain

- Does it have a distinct purpose?
  
- Does it own responsibilities that another domain should not own?
  
- Can we clearly describe what it excludes?
  
- Are its implementation characteristics separate from its responsibilities?
  
-  Does it have identifiable inputs and outputs?
  
- Does it communicate through well-defined contracts?
  
### Responsibility

- If we remove this thing, does the domain lose its purpose, or only one way of accomplishing its purpose?
  
### Characteristics

- Does this describe the implementation rather than ownership?
  
