# Windows-Side Operator Interface and Kinect Management
This folder contains the C# project necessary to communicate with the NAO and to computer pose matching.

This project requires Visual Studio 2022 or later with either Kinect 360 or Kinect One drivers installed on the system.

When running the program, it should be run from within Visual Studio, using the appropriate compile configuration for the Kinect that is installed and being used. So, if using an Xbox 360 Kinect, the Kinect360Debug or Kinect360Release compile configuration should be selected.

## Adding Additional Content

### Adding Lead Behaviors
If wanting to add additional lead action behaviors, add the custom behavior to the [Lead Actions](../LeadActions/) choregraphe project and re-upload the project to the NAO. Then add the new action name to the `LeadActions` list in [Constants.cs](./Kinect/Utilities/Constants.cs). Also confirm that the app ID of the re-uploaded project still matches the `CustomBehaviorsID` key value in [App.config](../Kinect/Kinect/App.config).

### Adding Audio to Audio Groups
If adding additional audio to the Audio groups defined in the [Audio Information](../AudioInformation.md), then simply match the appropriate naming convention, and increment the number of clips for the group in the audio group settings of the [App.config](./Kinect/App.config) file.

### Adding Single Audio Clips
This version is not easy and requires manually adding a function to call the clip to the `AudioHandler` class in [AudioHandler.cs](./Kinect/AudioHandler.cs), as well as adding a button and click event function to the [Main Window UI](./Kinect/MainWindow.xaml) and its [underlying partial class](./Kinect/MainWindow.xaml.cs), respectively.

The listing for single audio clips in [App.config](../Kinect/Kinect/App.config) is incomplete and does nothing.

## Resources
Resource information for getting setup with this repository. Includes links for required drivers and software from hopefully locations that wont disappear.

### XBOX 360 Kinect
1. [360 Kinect SDK](https://www.microsoft.com/en-US/download/details.aspx?id=40278)
2. [360 Kinect Developer Toolkit](https://www.microsoft.com/en-US/download/details.aspx?id=40276)
3. [Internet Archive 360 Kinect SDK & Toolkit (v1.8)](https://archive.org/details/kinect-software-developer-kit)

### XBOX One Kinect
1. [Kinect One Runtime/Drivers](https://www.microsoft.com/en-us/download/details.aspx?id=57578)
2. [Kinect One SDK](https://www.microsoft.com/en-US/download/details.aspx?id=44561)
3. [Internet Archive Kinect One SDK (DOES NOT INCLUDE REQUIRED RUNTIME)](https://archive.org/details/kinect-software-developer-kit)
4. [Internet Archive Kinect One Runtime/Drivers](https://archive.org/details/kinect-for-windows-v2-runtime-installers)