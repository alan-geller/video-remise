# REQUIREMENTS

## Deployment

### Software Platform

1. The system must run on recent Windows systems (Windows 11 or later).
1. The system may run on MacOS or Linux, but this is not required.
1. The system may run on Android, but this is not required.

### Computer Hardware

1. The system should work with any USB or HDMI camera.
1. The system should be able to support a single high-definition (1920x1280), 60fps video stream on a dual-core x64 CPU with 8GB of RAM.
1. The system should support both mouse and touch-screen control.

### Fencing Hardware

## Functional

### Streams

1. The system should support any number of video and audio streams, subject to CPU and memory constraints.
1. The system should support multiple video streams of differing quality (resolution or fps rate).

### Recording Control (Halts)

1. The system should use time stoppage information from the fencing hardware to determine halts.
1. The system should allow halts to be manually created by the user.
1. The system should continue recording a configurable amount of time after a halt.

### Playback

1. The system should support simultaneous playback of all streams.
1. The system should support a wide range of playback speeds, at least from 10% to 100% in steps of 10%.
1. The system should allow playback of prior halts, not just the most recent halt.
1. When playing back a halt, the system should start from a configurable amount of time before the halt to an independently configurable amount of time after the halt (see [above](#recording-control-halts)).

### Archival Storage

1. The system should support archival recording of matches that includes all of the live action time plus the configured time after a halt.