namespace YummyVerse.Scripts.Model.Interface
{
    /// <summary>
    /// 無操作の監視。一定時間何のイベントも起きなければ OnUserAbsent を発火させる。
    /// 人検知センサが入ったら、この実装を差し替えるだけでよい。
    /// </summary>
    public interface IIdleWatcher
    {
        /// <summary>監視の開始/停止。Attract 中は止めておく(来場者がいないのは当然のため)。</summary>
        void SetActive(bool active);

        /// <summary>操作があったことを手動で通知し、タイマーを巻き戻す。</summary>
        void NotifyActivity();

        float IdleTimeoutSeconds { get; set; }
    }
}
