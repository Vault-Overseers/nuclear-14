<p align="center"> <img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg" /></p>

**Nuclear 14** is a multiplayer survival role-playing game set in a post-nuclear apocalyptic world.

## Links

[Discord](https://discord.gg/4gGSWyNbQF) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.io/about/nightlies/)

## Documentation/Wiki

The SS14 [docs site](https://docs.spacestation14.io/) has documentation on SS14's content, engine, game design and more. This is a good resource for understanding how to contribute to every avenue of the project such as creating sprites, entities and maps, as well as contributing the the underlying systems of the game.

## Contributing

We are happy to accept contributions from anybody. Join the [Discord](https://discord.gg/4gGSWyNbQF) if you want to help. We've got a [list of issues / requests](https://github.com/Vault-Overseers/nuclear-14/issues) that need to be done and anybody can pick them up. Don't be afraid to ask for help either!

### Contributor Guidelines
To minimise conflicts with upstream when we update, please put all prototype changes in our directory Resources\Prototypes\Nuclear14 and all texture changes in Textures\Nuclear14 where possible. If you are unable to do so for whatever reason, please ping a maintaner and mention it in your pull request.

When porting content from another codebase or using textures / content from another creator, please use correct attribution for the license used as well as any copyright or source information. This is often recorded in the meta.json file for .RSI textures, and in a robust generic attribution (RGA) file for all other assets. This should be labelled attributions.yml in the given directory. See details on using the [RGA format here](https://github.com/space-wizards/RobustToolboxSpecifications/blob/master/RobustGenericAttribution/README.md).

## Building

1. Clone this repo.
2. Run `RUN_THIS.py` to init submodules and download the engine.
3. Compile the solution.

[More detailed instructions on building the project.](https://docs.spacestation14.io/getting-started/dev-setup)

## License

All code for the content repository is licensed under [MIT](https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT).

Most assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json). 

Note that some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.

Please note as a fork of SS14, we will us assets originally from SS13 from various codebases and communities. In all cases we will reference the codebase and the commit that the asset was present in, and IF possible will credit the original author. Most codebases are a result of iterations over many years so sometimes this information can become hard to record. SS14 is built on this same philosophy of forking and cross contribution so please take it as a show of flattery and in good faith. Thank you.

## Legal Disclaimer
Nuclear14 isn't endorsed by Bethesda Softworks, ZeniMax Media Inc or Microsoft, and doesn't reflect the views or opinions of any of these companies or anyone officially involved with making Fallout. Fallout and Bethesda Softworks and all related logos are trademarks or registered trademarks of Bethesda Softworks, Microsoft and all other relevant companies.
