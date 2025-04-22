# NetAgent

A powerful and flexible .NET framework for building AI agents with support for multiple LLM providers. NetAgent provides a modular architecture that allows you to easily integrate AI capabilities into your .NET applications.

## Table of Contents
- [Solution Structure](#solution-structure)
- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Extending the Framework](#extending-the-framework)
- [Error Handling](#error-handling)
- [Logging](#logging)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Solution Structure

The solution consists of several key projects:

### Core Projects
- **NetAgent.Abstractions**: Contains interfaces and base models for the framework
- **NetAgent.Core**: Core implementation of the agent system
- **NetAgent.Runtime**: Runtime execution engine for agents

### LLM Integration
- **NetAgent.LLM**: Base LLM integration interfaces
- **NetAgent.LLM.OpenAI**: OpenAI provider implementation
- **NetAgent.LLM.AzureOpenAI**: Azure OpenAI provider
- **NetAgent.LLM.Ollama**: Ollama provider for local LLM deployment
- **NetAgent.LLM.Factory**: Factory pattern for LLM provider initialization

### Memory Management
- **NetAgent.Memory.InMemory**: In-memory storage implementation
- **NetAgent.Memory.Redis**: Redis-based persistent storage

### Planning & Optimization
- **NetAgent.Planner.Default**: Default planning implementation
- **NetAgent.Planner.CustomRules**: Custom planning rules engine
- **NetAgent.Optimization**: Optimization strategies for agent performance
- **NetAgent.Strategy**: Strategic decision making components

### Tools & Evaluation
- **NetAgent.Tools.Standard**: Standard tool collection
- **NetAgent.Evaluation**: Evaluation and metrics collection

## Features

- **Multiple LLM Providers**:
  - OpenAI (GPT-3.5, GPT-4)
  - Azure OpenAI Service
  - Ollama (Local LLM deployment)
  
- **Memory Systems**:
  - In-memory storage
  - Redis persistence
  - Extensible memory provider interface
  
- **Advanced Planning**:
  - Default planner with optimization
  - Custom rules engine
  - Strategic decision making
  
- **Built-in Tools**:
  - DateTime handling
  - Weather information
  - HTTP requests
  - Calculator
  - Web search
  
- **Evaluation & Optimization**:
  - LLM-based evaluation
  - Prompt optimization
  - Performance metrics
