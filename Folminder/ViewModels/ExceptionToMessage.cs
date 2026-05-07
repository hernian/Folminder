using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace Folminder.ViewModels
{
    public sealed class ExceptionToMessage<TException> : ExceptionHandler.IErrorToMessage where TException : Exception
    {
        private readonly Func<TException, Message> _generate;

        public ExceptionToMessage(Func<TException, Message>　generate)
        {
            _generate = generate;
        }

        public bool Matches(Exception ex) => ex is TException;
        public Message Generate(Exception ex) => _generate((TException)ex);
    }
}
