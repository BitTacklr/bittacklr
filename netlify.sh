#!/bin/bash
sudo apt-get install mono-complete fsharp
mono packages/FAKE/tools/FAKE.exe build.fsx "$@"