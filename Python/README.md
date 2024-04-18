# NAO-side Code
This folder is the python 2 (for running on the NAO) and python 3 (for running on the computer) code necessary to run the project on the NAO. 

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

To upload the custom behaviors, you open the [LeadActions folder](../LeadActions/) with choregraphe, and upload to the robot from there. The uploaded AppID should be copied to the CustomBehaviorsID in [`app.config`](../Kinect/Kinect/App.config) file in the C# project so the code can tell the NAO which behavior to play. The behavior list is currently hardcoded into the [`Constants.cs`](../Kinect/Kinect/Utilities/Constants.cs) file. 

## Resources
Resource information for getting setup with this repository. Includes links for required drivers and software from hopefully locations that wont disappear.

### NAO v6
1. [NAO Software downloads page](https://www.aldebaran.com/en/support/nao-6/downloads-softwares)
2. [Internet Archive link for all NAO v6 software](https://archive.org/details/softbank-nao-v-6-software)
3. [NAO 2.8 Documentation](http://doc.aldebaran.com/2-8/index.html)