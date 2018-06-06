# SpeckleDynamo

![speckle](images/speckle.gif)

## Dev notes

The SpeckleDynamo repo is currently made up by the following projects:

- SpeckleCore - submodule
- SpeckleDynamo - main project with receiver and sender component
- SpeckleDynamoConverter - the converter logic, compiles into a dll that is loaded by SpeckleCore using reflection see [Converter.cs](https://github.com/speckleworks/SpeckleCore/blob/master/SpeckleCore/Converter.cs#L135)
- SpeckleDynamoFunctions - custom Dynamo nodes implementing the NodeModels interface must be calling methods in a separate dll in order to return something (other than basic types). This project contains methods called by the Receiver `AstFactory.BuildFunctionCall`.
- SpecklePopup - login/registration popup, currently a clone of the one in the Rhino repo, to be added as a submodule
- SpeckleDynamoExtension - a Dynamo extension to add a `Speckle` item to the Dynamo menu bar, the sole scope of this is to let users set and change their default Speckle account, in the future more advanced functionalities could be added via the extension



### Build instructions

Rebuild all should do just fine, since the SpeckleDynamoExtension is copying files to C:\Program Files running VS as admin might be required.

#### Debugging

Post build events have been set up to copy all required files into the Dynamo Core 1.3 folder.

Start actions have been set to launch Dynamo Sandbox 1.3.

*SpeckleDynamo* references all other projects a part from *SpeckleDynamoExtension*, debugging it you can debug the other projects as well.

*SpeckleDynamoExtension* is not being referenced by Speckle Dynamo and should be debugged separately.



## Current status



**!!! WORK IN PROGRESS !!!**



#### What's working

- account selection and connection (via the nodes and via an extension too)

- sender and receiver integration with core

- basic data conversion (created SpeckleDynamoConverter! > basic types, points lines)

- layers (basic implementation)

- dynamic inputs and outputs on sender/receiver



#### What's not working

- data structures other than a single item, so lists and trees need to be flattened and their structure needs to be stored in the layer topology
- any other geometric primitive other than points and lines
- ID input on receiver is not doing anything, should override the value in the text box
- various bugs
- 


## About Speckle

Speckle reimagines the design process from the Internet up: an open source (MIT) initiative for developing an extensible Design & AEC data communication protocol and platform. Contributions are welcome - we can't build this alone!
