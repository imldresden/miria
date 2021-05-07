# Welcome to MIRIA #

MIRIA (for Mixed Reality Interaction Analysis) is an Augmented Reality application and toolkit to facilitate the analysis of (spatial) interaction in AR environments. By enabling the visual exploration of such interaction data in the original environment, we hope to support the analysis process. More information about our research on MIRIA and an in-depth discussion of many design decisions can be found in our ACM CHI 2021 paper.

> [!IMPORTANT]
> This software is being developed for research use. It may contain bugs, including critical security flaws.
> Do not use this software for critical production purposes and only use it in closed, trusted networks.
> In accordance with the license terms, no warranty of any kind is given.

## Installation ##

### Prerequisites ###

MIRIA has been developed and tested with Mixed Reality Toolkit (MRTK) v. and Unity 2018.4. The MRTK is included in the repository and has already been set up for use MIRIA with a Microsoft HoloLens 2 AR device.

### Installation steps ###

To install MIRIA, please follow these steps:

1. Clone the repository
2. Open the project in UNITY 2018.4 (LTS)
3. If necessary, change the platform to Universal Windows Platform (_File -> Build Settings_) and build the project.
4. Open the generated solution in Visual Studio 2017/2019 and build and deploy the app.
5. All study data should be placed in a directory _miria_data_ located as follows:
    * UWP/WSA (e.g., HoloLens): _\<Objects3D\>\miria_data_, where _\<Objects3D\>_ is the corresponding KnownFolder (https://docs.microsoft.com/en-us/uwp/api/windows.storage.knownfolders) and is user writable when the device is connected via USB.
    * Win32 (e.g., Unity Editor or Standalone Player): _%userprofile%\AppData\LocalLow\Interactive Media Lab Dresden\MIRIA\miria_data_

> [!NOTE]
> Currently, the application assumes that each client has *exactly* the same study data.

## Usage ##

After starting MIRIA, each client either start a new session (Start Session) or join an existing session (Join Session) in the local network.

### Starting a session & single user  ###

To start a new session, click the (Start Session) button. The server will be announced in the local network and other clients can join.

### Joining a session ###

To join an existing session, click on the name of the session in the session browser and then click the (Join Session) button. Any newly created session should show up in the session browser after a few seconds. If a session does not show up in the browser, make sure that all devices are in the same local network and that no firewall prevents the connection.

After joining a session, the current application state is synchronized between the server and the new client. This includes the id of the currently loaded session, the world anchor state, the timeline state, and the configuration of the visualizations.

## Development ##

MIRIA can easily be used as outlined above. For specific or more complex use cases, MIRIA can also be extended. and finally, you can of course use the subsystems independently and build your own application using MIRIA building blocks.

> [!NOTE]
> In future versions, we aim to further reduce dependencies between MIRIA's components.

### Adding new visualizations types ###

To extend MIRIA by adding a new type of visualization you should do the following:

1. Create a new visualization class inheriting from _AbstractView_ and possibly implementing _IConfigurableVisualization_.
2. Add a new value to the _VisType_ enum that represents the new type of visualization.
3. Create a new prefab in the Unity editor that contains all necessary primitives for your visualization and add the visualization logic you wrote to this prefab. Fill all necessary fields.
4. Extend the _VisualizationManager_ script and prefab to be able to create your visualization during runtime.

### Adding new data sources ###

Currently, MIRIA can read data from CSV files. If the existing importer does not suit your needs, you can write your own.

1. Inherit from _AbstractDataProvider_.
2. Update the _DataManager_ field of the _Services_ prefab to point to the new importer.

## Getting support ##

If you find any bugs or would like to see additional features, please create a ticket in the issue tracker. For general feedback, contact the maintainer at wolfgang.bueschel AT tu-dresden.de. Please understand that we do not have the resources to provide general support for this software.

## Citing MIRIA ##

If you would like to cite MIRIA in your research, please cite our CHI '21 paper:

_Wolfgang Büschel, Anke Lehmann, and Raimund Dachselt. 2021. MIRIA: A Mixed Reality Toolkit for the In-Situ Visualization and Analysis of Spatio-Temporal Interaction Data. In CHI Conference on Human Factors in Computing Systems (CHI ’21), May 8–13, 2021, Yokohama, Jap an. ACM, New York, NY, USA, 15 pages. https://doi.org/10.1145/3411764.3445651_