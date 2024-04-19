# NAO pose following using Xbox Kinect
This version of the project was developed for a prototype turn-taking mirroring game with residents at a skilled nursing facility with a NAO robot. It is designed to be human-operated in a Wizard of Oz (woz) fashion, with certain commentary and behaviors being automatically driven by the current turn and the type of action selected.

## Laptop Setup

### Requirements
1. Visual Studio 2022 or later with either Kinect 360 or Kinect One drivers installed
2. Network connection to the NAO Robot


## NAO First Time Setup
Initial NAO connection can be completed with the included ethernet cable. However, to complete the setup in Robot Settings, a wireless network (with access to the broader internet!) must be provided, and the ethernet cable disconnected before you can progress. It might require restarting the NAO/Robot Settings program.

The "Create Account" link goes to a bad URL. Instead create an account [here](https://cloud.aldebaran-robotics.com/). Then log into it (it might take a couple tries) and finish the initial setup.

### NAO Prerequisites

First, install `pip` on the Nao. From an SSH session to the Nao:

```
wget https://bootstrap.pypa.io/pip/2.7/get-pip.py
python get-pip.py
```

Next, install the python dependencies from this repository. From your computer:

```
scp requirements.txt requirements-2.7.txt nao@nao.local:~/
ssh nao@nao.local
pip install -r requirements.txt
pip install -r requirements-2.7.txt
```


## Set up Windows Laptop for uploading to NAO
You'll need to use windows subsystem for linux, so you have access to rsync. (You could also copy the rsync files to the specific folders for Git for Windows bash control, but let's go with wsl instead.) From administrator access in command prompt type `wsl --install`

When it finishes, reboot the system. It will open a new window asking you to setup your linux account. It should have python 3 installed. We will install pip, and also set an alias for python3

```
sudo apt-get update
sudo apt-get install python3-pip
echo "alias python='python3'" >> ~/.bashrc
```

Then, from the nao-pose-following project folder, run the following commands to finish setting up prerequisites.

```
cd ./Python
pip install -r requirements.txt
```

## Uploading Code to NAO
First make sure all the audio files are in the `Python/audio` folder.

Use `./bin/load_on_robot.py` to install the mirroring code onto your Nao robot, from the Python folder:

```
./bin/load_on_robot.py
```

To upload the custom behaviors, you open the [LeadActions folder](./LeadActions/)  with choregraphe, and upload to the robot from there. The uploaded AppID should be copied to the CustomBehaviorsID in [`app.config`](./Kinect/Kinect/App.config) file in the C# project so the code can tell the NAO which behavior to play. The behavior list is currently hardcoded into the [`Constants.cs`](./Kinect/Kinect/Utilities/Constants.cs) file. 

## Running the System
1. In Visual Studio, select either the Kinect360 or KinectOne compile configuration, depending on which Kinect is installed with drivers.
2. Press the run button
3. SSH into the NAO robot, and navigate to `~/mirror_game/`
4. Run `./mirror_game.py`
5. Press the Start button in the C# application.

The NAO should stand up, and packet messages should be printing to the ssh terminal.

## Adding new audio files
The audio files were made using Amazon Polly, for the neural Matt voice and neural Joanna voice. The audio files are located in the audio folder with the python code. On install the files in this folder are uploaded to the NAO.

When generating new audio for Matt, once the file has been downloaded, it must be run through the [Audacity](https://www.audacityteam.org/) amplification filter in order to increase the volume to the actual in-data maximum, as this voice is particularly quiet.

If adding more options to an existing audio group, such as adding more jokes, the file must match the naming scheme described in [Audio Information](./AudioInformation.md), uploaded to the NAO, and the number of audio options for that group should be updated in the [`app.config`](./Kinect/Kinect/App.config) file in the C# project.

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

### NAO v6
1. [NAO Software downloads page](https://www.aldebaran.com/en/support/nao-6/downloads-softwares)
2. [Internet Archive link for all NAO v6 software](https://archive.org/details/softbank-nao-v-6-software)
3. [NAO 2.8 Documentation](http://doc.aldebaran.com/2-8/index.html)