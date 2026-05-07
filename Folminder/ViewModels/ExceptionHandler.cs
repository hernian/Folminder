namespace Folminder.ViewModels
{
    public class ExceptionHandler
    {
        public interface IErrorToMessage
        {
            bool Matches(Exception ex);
            Message Generate(Exception ex);
        }

        private readonly IReadOnlyList<IErrorToMessage> _rules;
        private readonly Action<Message> _handleMessage;

        public ExceptionHandler(IReadOnlyList<IErrorToMessage> rules, Action<Message> handleMessage)
        {
            _rules = rules;
            _handleMessage = handleMessage;
        }

        public void CommandHarness(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var rule = _rules.FirstOrDefault(r => r.Matches(ex));

                Message message = rule != null
                    ? rule.Generate(ex)
                    : new Message(MessageKind.Error, $"予期しないエラーが発生しました: {ex.Message}");
                _handleMessage(message);
            }
        }
    }
}
