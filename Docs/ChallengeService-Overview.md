# Overview

```mermaid
graph TD
    A[Challenge Creation] --> B(Language?);
    B --> Python --> C[PythonChallengeTests Repo];
    B --> NodeJs --> D[NodeJsChallengeTests Repo];
    C --> E[programming-challenges];
    D --> E[programming-challenges];
    E --> F[python-docker-image];
    E --> G[nodejs-docker-image];
    Z[Challenge Submitted] --> Y(Language?);
    Y --> X[Python];
    Y --> U[NodeJs];
    U --> G;    
    X --> F;
    F --> AA[Queue]
    G --> AA
    AA --> Inject[Insert code into new container]
    Inject --> Test[Execute Entrypoint from Test Record]
    Test --> Parse[Parse Report and cleanup]
    Parse --> Generate[Compile and send user-facing reports]
```

The challenge service is the most complex service we offer. It has a lot of moving parts.

For each language we want to support, they are added as submodules in the `Programming Challenges` Repo. Through CI/CD we can automate building and pushing docker images for each module!

```mermaid
graph TD
    PythonRepo[Python Test Repo] --> Central[Programming Challenges Repo]
    NodeJsRepo[NodeJS Test Repo] --> Central
```

## What is a challenge?
A [challenge](https://github.com/Programming-Simplified-Community/CommunityBot/blob/main/Data/Challenges/ProgrammingChallenge.cs#L8-L37) represents what we want from our users. Explains the specifications that they must meet.

## Tests
It is important that the tests we create match our challenge specifications verbatim! We must be fair to our users in that aspect.

In the database, we can [link](https://github.com/Programming-Simplified-Community/CommunityBot/blob/main/Data/Challenges/ProgrammingChallenge.cs#L39-L71) tests from our repos to a challenge!

Each test has an associated language, docker image, entrypoint, and starting file location. Helps us keep things flexible.