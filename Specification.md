# SPECIFICATION

## Platform and Language

Unless it turns out that we can't avoid catastrophic GC events, the system will be built on the .NET Framework 6.0 (or higher).
This is an open-source, cross-platform version of .NET, so it is possible that at least the core of an application developed for Windows will be portable to other operating systems.

We will use C# version 10.0 (or higher).
While there are advantages to the functional capabilities of F#, sticking to C# ensures that this project will be accessible to more users.
In addition, the use of immutable data structures that F# encourages may be counterproductive when we are trying to avoid memory allocations.

## Software Design

### Stream Hierarchy

`Steam` is an abstract base class with at least two concrete subclasses, `VideoStream` and `AudioStream`.
The base class will provide the following methods:

| Method or Property | Description |
|--------------------|-------------|
| `StartRecording`   | Starts recording the stream to memory. |
| `HaltRecording`    | Marks a halt (including the configurable delay) and pauses recording. |
| `StopRecording`    | Signals the end of the bout, flushes the stream to disk if desired, and cleans up. |
| `Playback`         | Plays the stream back to a target window pane, at the specified speed. |

## Deployment
