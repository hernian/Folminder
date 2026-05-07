using System;
using System.Collections.Generic;
using System.Text;

namespace Folminder.ViewModels
{
    /// <summary>
    /// トーストメッセージ表示イベントの引数
    /// </summary>
    public class MessageRequestedEventArgs(Message message) : EventArgs
    {
        public Message Message { get; } = message;
    }
}
