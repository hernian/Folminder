using System;
using System.Collections.Generic;
using System.Text;

namespace Folminder.ViewModels
{
    public record Message(MessageKind Kind, string Body);
}
