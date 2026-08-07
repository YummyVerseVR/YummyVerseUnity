using System;
using R3;
using UnityEngine;
using YummyVerse.Scripts.Model.Interface;
using YummyVerse.Scripts.Model.Struct;

namespace YummyVerse.Scripts.Model
{
    public class GameCommandBus : IGameCommandBus, IDisposable
    {
        private readonly Subject<GameCommandId> _onCommand = new();
        public Observable<GameCommandId> OnCommand => _onCommand;

        public void Request(GameCommandId command)
        {
            if (command == GameCommandId.None) return;
            Debug.Log($"[GameCommand] {command}");
            _onCommand.OnNext(command);
        }

        public void Dispose()
        {
            _onCommand?.Dispose();
        }
    }
}
