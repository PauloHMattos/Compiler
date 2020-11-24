#!/bin/bash

# Vars
slndir="$(dirname "${BASH_SOURCE[0]}")/"

# Restore + Build
dotnet build "$slndir/Compiler.Application" --nologo || exit

# Run
dotnet run -p "$slndir/Compiler.Application" --no-build -- "$@"