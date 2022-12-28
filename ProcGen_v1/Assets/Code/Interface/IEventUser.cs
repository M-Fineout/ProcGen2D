using Assets.Code.Global;
using Assets.Code.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.Interface
{
    /// <summary>
    /// Marks the implementing class as a client of <see cref="EventBus"/>
    /// </summary>
    public interface IEventUser
    {
        Dictionary<GameEvent, Action<EventMessage>> Registrations { get; set; }
        void RegisterEvents();
        void UnregisterEvents();
    }
}
