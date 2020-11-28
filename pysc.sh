#!/bin/bash

# Vars
slndir="$(dirname "${BASH_SOURCE[0]}")/"

# Restore + Build
dotnet build "$slndir/src/Compiler.Application" --nologo || exit

# Run
dotnet run -p "$slndir/src/Compiler.Application" --no-build -- "$@"