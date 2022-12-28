using UnityEngine;

namespace Assets.Code.Util
{
    /// <summary>
    /// Handles the lifecycles of non gameobjects
    /// </summary>
    public class Container
    {
        public static Container instance;
        private EventBus EventBus { get; set; }
        private GameLogConfiguration GameLogConfiguration { get; set; }
        public MovementCoordinator MovementConductor { get; set; }

        public static void Build()
        {
            instance = new Container();
        }

        public Container()
        {
            Debug.Log("Building Container");
            Fill();
        }

        private void Fill()
        {
            EventBus = new EventBus();
            GameLogConfiguration = new GameLogConfiguration();
            MovementConductor = new MovementCoordinator();
        }
    }
}
