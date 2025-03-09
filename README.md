# Nuclear 14
Nuclear 14 is the original Fallout fork on Space Station 14 created by Peptide90 in 2022 and a collection of other valuable contributors. It uses assets from various Fallout13 (F13)(SS13) forks as well as brand new assets created by our community. The location and theme of Nuclear14 differs from F13 for a variety of reasons but mostly to give people something new to experience rather than rehashing the old again. Thanks to the SS14 engine and our Upstream repository, Einstein Engines, we bring something highly modular to the community to enjoy. The codebase has been licensed as AGPLv3 so that the fork and its developments can be enjoyed by all.

The SS13 remake curse and F13 curse have been broken, come check out the community on [discord here](https://discord.gg/4gGSWyNbQF) and consider contributing. Links to the official servers can be found on the discord too or via the launcher at `ss14://game.nuclear14.com:1212`

N14 is based on Einstein Engines.

## Building

Refer to [the Space Wizards' guide](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html) on setting up a development environment for general information, but keep in mind that Einstein Engines is not the same and many things may not apply.
We provide some scripts shown below to make the job easier.

### Build dependencies

> - Git
> - .NET SDK 9.0.101


### Windows

> 1. Clone this repository
> 2. Run `git submodule update --init --recursive` in a terminal to download the engine
> 3. Run `Scripts/bat/buildAllDebug.bat` after making any changes to the source
> 4. Run `Scripts/bat/runQuickAll.bat` to launch the client and the server
> 5. Connect to localhost in the client and play

### Linux

> 1. Clone this repository
> 2. Run `git submodule update --init --recursive` in a terminal to download the engine
> 3. Run `Scripts/sh/buildAllDebug.sh` after making any changes to the source
> 4. Run `Scripts/sh/runQuickAll.sh` to launch the client and the server
> 5. Connect to localhost in the client and play

### MacOS

> I don't know anybody using MacOS to test this, but it's probably roughly the same steps as Linux

## License

Please read the [LEGAL.md](./LEGAL.md) file for information on the licenses of the code and assets in this repository.
