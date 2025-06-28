# BrokenHelper

This repository contains a WPF application built with .NET 8.0. The project uses `SharpPcap` and `PacketDotNet` to capture and analyze network packets.

## Prerequisites

- .NET 8.0 SDK (or later)
- Visual Studio 2022 (optional) for a full IDE experience

## Building

To build the application from the command line, run:

```bash
dotnet build BrokenHelper.sln
```

This restores all NuGet packages and compiles the `BrokenHelper` project.

## Running

Run the application with:

```bash
dotnet run --project BrokenHelper.csproj
```

Alternatively, open `BrokenHelper.sln` in Visual Studio and press `F5` to start debugging.

## Packet Capture Permissions

The application starts a packet listener using `SharpPcap`. Capturing packets requires administrator privileges. If the listener fails with a `PermissionDenied` error, run the application as administrator or install Npcap/WinPcap with the option to allow non-administrator captures.

