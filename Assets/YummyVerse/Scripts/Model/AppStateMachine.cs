using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class AppStateMachine : IAppStateMachine, IDisposable
    {
        private readonly ReactiveProperty<AppState> _current = new(AppState.Attract);
        public ReadOnlyReactiveProperty<AppState> Current => _current;

        public bool TrySet(AppState next)
        {
            var from = _current.Value;
            if (from == next) return true;

            if (!IsAllowed(from, next))
            {
                Debug.LogError($"[AppState] 不正な遷移: {from} -> {next}");
                return false;
            }

            Debug.Log($"[AppState] {from} -> {next}");
            _current.Value = next;
            return true;
        }

        // Attract への復帰は常に許可(来場者の離脱はどの状態でも起こる)。
        // それ以外は一方向のみ。
        private static bool IsAllowed(AppState from, AppState to)
        {
            if (to == AppState.Attract) return true;

            return (from, to) switch
            {
                (AppState.Attract, AppState.Tutorial) => true,
                (AppState.Tutorial, AppState.FreePlay) => true,
                (AppState.FreePlay, AppState.Outro) => true,
                _ => false
            };
        }

        public void Dispose()
        {
            _current?.Dispose();
        }
    }
}
