using Kinect.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect.Model
{
    // A starter class for handling our cycling queue adventure
    public abstract class CycleQueue
    {
        protected List<string> itemList;
        protected Queue<string> itemQueue = new Queue<string>();


        protected void ResetQueue()
        {
            this.itemList.Shuffle();
            this.itemQueue = new Queue<string>(this.itemList);
        }

        protected string GetNewItem()
        {
            try
            {
                return this.itemQueue.Dequeue();
            }
            catch
            {
                this.ResetQueue();
                return this.GetNewItem();
            }
        }
    }
}
