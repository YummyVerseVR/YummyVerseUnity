using System;
using PCVR.Model.Interfaces;
using R3;

namespace PCVR.Model{
    public class QRCodeManager: IQRCodeManager
    {
        public ReactiveProperty<Guid> UserId { get; private set; }  = new ReactiveProperty<Guid>(Guid.NewGuid());
    }
}