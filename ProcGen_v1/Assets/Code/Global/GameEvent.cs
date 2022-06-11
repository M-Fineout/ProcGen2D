namespace Assets.Code.Global
{
    public enum GameEvent
    {
        //HeartBeat communicates to the GameManager that BoardManager is ready for requests
        //For now, this is a roundabout way to NOT run all of board setup in the Unity Start method (This is subject to change)
        HeartBeating,

        SceneLoaded,
        LevelCompleted,

        EmptyTilesRequested,
        EmptyTilesFound,
        WallTilesRequested,
        WallTilesFound,
        BlueprintsRequested,
        BlueprintsOutgoing,

        PlayerHit,
        PlayerAttack,
        PlayerAttackEnded,

        EnemyDefeated
    }
}
