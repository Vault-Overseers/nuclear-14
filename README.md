# Base Station 14

Give your [Space Station 14](https://github.com/space-wizards/space-station-14) fork a stable foundation.

**Base Station 14** provides stable branches of Space Station 14 that you can use to build your fork.
A stable branch is cut from a specific upstream revision and is only updated with bug fixes and low-risk content (e.g. sprites and YAML) changes that do not break your fork's content.

## Using Base Station 14 In Your Fork
To start a new fork using **Base Station 14**, simply clone this repository and check out the branch you want to start with.

If you have an existing fork:

1. Add **Base Station 14** as a remote in your repository.
2. Then, find the closest **Base Station 14** branch to your last upstream rebase or merge.
3. Rebase or merge with the **Base Station 14** branch that you want to use.

## Stable Branches

- `basestation/v1/stable`: Cut from upstream [9a38736c3c](https://github.com/space-wizards/space-station-14/commit/9a38736c3c) on 2022-10-22 (*To see changes:* `git log --oneline 9a38736c3c..basestation/v1/stable`)

## Contributing
Stable branches will only be updated with upstream bug fixes and low-risk content updates.

We accept patches (`git format-patch`) or fast-forward-only pull requests to the `current` branch (e.g. `basestation/v1/current`).

If a bug fix is not available upstream, we will consider it for inclusion if it is also upstreamed, if applicable.

## Patch Kits
**Base Station 14** consists of only upstream content.
However, common content from the **Nanotrasen Fork Network** targeting specific **Base Station 14** stable branches are available.
To add patch kit content, cherry-pick the appropriate patch kit matching your stable branch version.

- `ladders`: allow your players to travel from location to location or map to map: `git cherry-pick basestation/v1/ladders`
