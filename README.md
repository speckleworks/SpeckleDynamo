# Speckle for Dynamo

[![Build status](https://ci.appveyor.com/api/projects/status/lm4alrukr13lm027?svg=true)](https://ci.appveyor.com/project/SpeckleWorks/speckledynamo)

## ⚠️ IMPORTANT ⚠️

Speckle v1 has reached EOL and is no longer mainatined. Check out our brand new 👉 [Speckle v2](https://github.com/specklesystems)!
Read more about the announcemnt [here](https://speckle.systems/blog/speckle2-vision-and-faq) and check out or new [website](https://speckle.systems).


![speckle-demo](https://user-images.githubusercontent.com/2679513/41601159-99a0499a-73cf-11e8-90b9-36b43a822076.gif)

Speckle for Dynamo is out! 

---
## Contents

- [Installation](#installation)
- [Usage](#usage)
- [Custom UserData](#custom-userdata)
- [Bugs and feature requests](#bugs-and-feature-requests)
- [Building Speckle for Dynamo](#building-speckle-for-dynamo)
    - [Requirements](#requirements)
    - [Dev Notes](#dev-notes)
    - [Building Process](#building-process)
    - [Debugging](#debugging)
- [About Speckle](#about-speckle)
- [Credits](#credits)

## Installation

> The [latest build is here](https://github.com/speckleworks/SpeckleInstaller/releases/latest). Be sure to check out the [getting started guide](https://speckle.works/log/speckle-guide/) to get up to speed with some basics.

## Usage

Speckle for Dynamo is currently made up by 7 components:
- **I/O** nodes:
    - The **Sender** lets you send data to Speckle receivers.
    - The **Receiver** lets you receive data from Speckle senders.
    - The **Streams** lets you select a Stream ID from all the available streams on your account.
- **UserData** nodes:
    - The **UserData.Set** lets you set a dictionary as custom properties to any geometry element.
    - The **UserData.Get** returns a dictionary with previously custom properties set in a geometry, if any.

**Speckle Accounts** lets you manage your Speckle accounts and set the default account for new nodes, it can be accessed from: View > Speckle Accounts

![1529327886901](https://user-images.githubusercontent.com/2679513/41541683-f199855a-730a-11e8-8b11-da7b5cc14a76.png)

## Custom UserData

UserData is a way of adding *extra* information/properties to any Dynamo Geometry, making use of their internal `Tag` property. These custom properties are set using Dynamo dictionaries.

The SpeckleConverter accepts nested dictionaries to be sent, as well as any geometry that can be currently serialized.

![Custom UserData](images/userData.gif)

## Bugs and feature requests

Please submit a new [issue](https://github.com/speckleworks/SpeckleDynamo/issues)!
**The Receiver will often show a warning regarding `Dictionary.ByKeysValues operation failed`. [We are aware of that](https://github.com/speckleworks/SpeckleDynamo/issues/20) and are working with the Dynamo team to fix it. Nothing to worry about for the moment!**

---

## Building Speckle for Dynamo

### Requirements

- Visual Studio 2017
- .NET Framework 2.0+ installed (it might be missing on Windows 8/10)

### Dev Notes

The SpeckleDynamo repo is currently made up by the following projects:

- SpeckleCore - submodule
- SpeckleDynamo - main project with receiver and sender component
- SpeckleDynamoConverter - the converter logic, compiles into a dll that is loaded by SpeckleCore using reflection see [Converter.cs](https://github.com/speckleworks/SpeckleCore/blob/master/SpeckleCore/Converter.cs#L135)
- SpeckleDynamoFunctions - custom Dynamo nodes implementing the NodeModels interface must be calling methods in a separate dll in order to return something (other than basic types). This project contains methods called by the Receiver `AstFactory.BuildFunctionCall`. It also hosts ZeroTouch nodes for Set and Get UserData.
- SpecklePopup - login/registration popup, currently a clone of the one in the Rhino repo, to be added as a submodule
- SpeckleDynamoExtension - a Dynamo extension to add a `Speckle` item to the Dynamo menu bar, the sole scope of this is to let users set and change their default Speckle account, in the future more advanced functionalities could be added via the extension

### Building Process

- Clone/fork the repo
- Restore all Nuget package missing on the solution.
- If SpeckleCore hasn't been cloned, running ` git submodule update --remote` on the command line should clone the submodule along.
- Set SpeckleDynamo as start project and rebuild all

### Debugging

- **Post build events** have already been set up to copy all required files into the Dynamo Core 2.0 folder.
- A **start action** has been set to launch Dynamo Sandbox 2.0. 
- **These can be changed in the .csproj file, but please don't set them from Visual Studio.**

*SpeckleDynamo* references all other projects, debugging it you can debug the other projects as well.

## About Speckle

Speckle reimagines the design process from the Internet up: an open source (MIT) initiative for developing an extensible Design & AEC data communication protocol and platform. Contributions are welcome - we can't build this alone!


## Credits

[Dimitrie](https://github.com/didimitrie), [Luis](https://github.com/fraguada), [Matteo](https://github.com/teocomi), [Alvaro](https://github.com/alvpickmans)
