using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service.Model
{
    [AddINotifyPropertyChangedInterface]
    public class Aria2Node: BaseNotificationModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        

        public string URL { get; set; }

        //public string Token { get; set; }


        private string _Token = "";
        public string Token
        {
            get => _Token;
            set => _Token = value;
        }

    }
}
