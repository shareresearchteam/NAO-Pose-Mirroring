using Kinect.Audio;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

/*
 * AudioTypeProperties and AudioSingleProperties are classes to define the configuration layout 
 * characteristics we want for the audio groups and unique audio clips that we will call. These are then 
 * linked to the information in App.config through the AudioTypePropertiesCollection and AudioSinglePropertiesCollection.
 * AudioSettings us then used to provide a way to call this configuration information from within our AudioCollection
 * code to generate the information for the audio groups 
 * and AudioHandler structure for providing a variety of audio calls for different tasks, and easily extending 
 * the number of audio queues in each audio group
 */

namespace Kinect.Model
{
    // We use these to be able to define our groups in the configuration, so we can set properties/etc
    public class AudioTypeProperties : ConfigurationElement
    {
        [ConfigurationProperty("Type", DefaultValue = AudioType.DrawIn, IsRequired = true)]
        public AudioType Type { get { return (AudioType)(int)this["Type"]; } set { value = (AudioType)(int)this["Type"]; } }
        [ConfigurationProperty("ClipCount", DefaultValue = 1, IsRequired = true)]
        public int ClipCount { get { return (int)this["ClipCount"]; } set { value = (int)this["ClipCount"]; } }
        [ConfigurationProperty("IsAudioAsync", DefaultValue = true, IsRequired = true)]
        public bool IsAudioAsync { get { return (bool)this["IsAudioAsync"]; } set { value = (bool)this["IsAudioAsync"]; } }
        [ConfigurationProperty("IsCallResponse", DefaultValue = true, IsRequired = true)]
        public bool IsCallResponse { get { return (bool)this["IsCallResponse"]; } set { value = (bool)this["IsCallResponse"]; } }
    }

    public class AudioSingleProperties : ConfigurationElement
    {
        [ConfigurationProperty("ClipName", DefaultValue = "", IsRequired = true)]
        public string ClipName { get { return (string)this["ClipName"]; } set { value = (string)this["ClipName"]; } }
        [ConfigurationProperty("ButtonName", DefaultValue = "", IsRequired = true)]
        public string ButtonName { get { return (string)this["ButtonName"]; } set { value = (string)this["ButtonName"]; } }
        [ConfigurationProperty("IsAudioAsync", DefaultValue = true, IsRequired = true)]
        public bool IsAudioAsync { get { return (bool)this["IsAudioAsync"]; } set { value = (bool)this["IsAudioAsync"]; } }
    }


    // Everything down here is just a bunch of element linking into the config hell. Don't worry about it.

    // Define the UrlsCollection that contains the 
    // UrlsConfigElement elements.
    // This class shows how to use the ConfigurationElementCollection.
    public class AudioTypePropertiesCollection : ConfigurationElementCollection
    {

        public AudioTypePropertiesCollection()
        {
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new AudioTypeProperties();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((AudioTypeProperties)element).Type;
        }

        public AudioTypeProperties this[int index]
        {
            get
            {
                return (AudioTypeProperties)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public int IndexOf(AudioTypeProperties url)
        {
            return BaseIndexOf(url);
        }

        public void Add(AudioTypeProperties url)
        {
            BaseAdd(url);

            // Your custom code goes here.
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);

            // Your custom code goes here.
        }

        public void Remove(AudioTypeProperties url)
        {
            if (BaseIndexOf(url) >= 0)
            {
                BaseRemove(url.Type);
                // Your custom code goes here.
                Console.WriteLine("UrlsCollection: {0}", "Removed collection element!");
            }
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);

            // Your custom code goes here.
        }

        public void Clear()
        {
            BaseClear();

            // Your custom code goes here.
            Console.WriteLine("UrlsCollection: {0}", "Removed entire collection!");
        }
    }

    // Define the UrlsCollection that contains the 
    // UrlsConfigElement elements.
    // This class shows how to use the ConfigurationElementCollection.
    public class AudioSinglePropertiesCollection : ConfigurationElementCollection
    {

        public AudioSinglePropertiesCollection()
        {
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new AudioSingleProperties();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((AudioSingleProperties)element).ClipName;
        }

        public AudioSingleProperties this[int index]
        {
            get
            {
                return (AudioSingleProperties)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public int IndexOf(AudioSingleProperties url)
        {
            return BaseIndexOf(url);
        }

        public void Add(AudioSingleProperties url)
        {
            BaseAdd(url);

            // Your custom code goes here.
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);

            // Your custom code goes here.
        }

        public void Remove(AudioSingleProperties url)
        {
            if (BaseIndexOf(url) >= 0)
            {
                BaseRemove(url.ClipName);
                // Your custom code goes here.
                Console.WriteLine("UrlsCollection: {0}", "Removed collection element!");
            }
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);

            // Your custom code goes here.
        }

        public void Clear()
        {
            BaseClear();

            // Your custom code goes here.
            Console.WriteLine("UrlsCollection: {0}", "Removed entire collection!");
        }
    }


    public class AudioSettings : ConfigurationSection
    {

        //public static List<AudioTypeProperties> AudioGroups => ConfigurationManager.GetSection("AudioSettings/AudioGroups") as List<AudioTypeProperties>;
        public static AudioSettings audioSettings => ConfigurationManager.GetSection("AudioSettings") as AudioSettings;

        //public static List<AudioSingleProperties> AudioSingles => ConfigurationManager.GetSection("AudioSettings/AudioSingles") as List<AudioSingleProperties>;

        //public static List<string> AudioVoices => ConfigurationManager.GetSection("AudioSettings/AudioVoices") as List<string>;

        // Declare the UrlsCollection collection property.
        [ConfigurationProperty("AudioGroups", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(AudioTypePropertiesCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public AudioTypePropertiesCollection AudioGroups
        {
            get
            {
                AudioTypePropertiesCollection audioCollection =
                    (AudioTypePropertiesCollection)base["AudioGroups"];

                return audioCollection;
            }

            set
            {
                AudioTypePropertiesCollection audioCollection = value;
            }
        }

        // Declare the UrlsCollection collection property.
        [ConfigurationProperty("AudioSingles", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(AudioSinglePropertiesCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public AudioSinglePropertiesCollection AudioSingles
        {
            get
            {
                AudioSinglePropertiesCollection audioCollection =
                    (AudioSinglePropertiesCollection)base["AudioSingles"];

                return audioCollection;
            }

            set
            {
                AudioSinglePropertiesCollection audioCollection = value;
            }
        }

    }
}
