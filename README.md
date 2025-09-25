# Greg.Xrm.Command ⁓ aka PACX 🔧
![image](https://github.com/neronotte/Greg.Xrm.Command/assets/1436173/23b01073-6f13-46cd-a6c6-b5bd264ed442)


## Overview 💻

**PACX** is a free to use, **open source**, command line based, utility belt for Dataverse.
It's aim is to extend the capabilities of [PAC CLI](https://learn.microsoft.com/en-us/power-platform/developer/cli/introduction?tabs=windows) providing a lot of commands designed by **Power Platform Developers** to:

- help with the automation of repetitive tasks 🤖
- provide an easy access to hidden gems that are provided by the platform only via API 🫣
- make development and deployment faster and more efficient 🚀

It's also a lot more than that. Built with an [XrmToolbox](https://www.xrmtoolbox.com/) like plugin-based approach in mind, it is also an easy-to-use platform to develop your own tools and extensions.

## Installation 🛠️

The tool can be installed as a `dotnet` global tool using the following command:

```powershell
dotnet tool install -g Greg.Xrm.Command
```

To update the tool to a newer version

```powershell
dotnet tool update -g Greg.Xrm.Command
```

## Usage 🚀

You can get the list of the available commands by running:

```powershell
pacx --help
```

- For the full list of commands please [visit the wiki](https://github.com/neronotte/Greg.Xrm.Command/wiki) section of the current repository.
- For articles, how-tos, tutorials, or real life usage examples [follow my LinkedIn](https://www.linkedin.com/in/riccardogregori/) profile or [my dev.to page](https://dev.to/_neronotte).
- Take a look on [episode 107 of XrmToolCast](https://youtu.be/r16vbSdeFLk?si=HYpoh-3QyrW1S41Q) where I've had a really interesting conversation with Scott Durow and Daryl LaBar about PACX aim, capabilities, and potentials.

## Extensions 🧩

You can extend PACX capabilities in 2 different ways:

- 🌟 [Extending the PACX core features](https://github.com/neronotte/Greg.Xrm.Command/wiki/How-to-contribute)
- 📦 [Creating your own Tools](https://github.com/neronotte/Greg.Xrm.Command/wiki/Tools-for-PACX): a tool is a set of PACX commands packaged in a single dll file that can be deployed locally for single use scenarios, or can be made available to the community via NuGet. Follow the instructions provided by the wiki page linked above to learn how to build, package and deploy your PACX Tools. **This is the preferred approach to add context specific commands**.

If you have created an interesting Plugin and you want to spread the word to the community, [🗣️ reach out to me](https://github.com/neronotte/Greg.Xrm.Command/discussions/new?category=show-and-tell) and I'll be glad to sponsor the tool in my pages!

## Core Contributors 👨🏻‍💻

-   Riccardo Gregori (@\_neronotte)
-   Simone Giubbarelli (@SimonGiubs)
-   Francesco Catino (@reloweb)
