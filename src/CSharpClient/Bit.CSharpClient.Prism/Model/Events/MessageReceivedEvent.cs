using Newtonsoft.Json.Linq;
using Prism.Events;

namespace Bit.Model.Events
{
    public class MessageReceivedEvent : PubSubEvent<MessageReceivedEvent>
    {
        public string Key { get; set; }

        public string Body { get; set; }

        JObject _jObject;

        public JObject AsJObject()
        {
            _jObject = _jObject ?? JObject.Parse(Body, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });
            return _jObject;
        }

        JToken _jToken;

        public JToken AsJToken()
        {
            _jToken = _jToken ?? JToken.Parse(Body, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });
            return _jToken;
        }

        JArray _jArray;

        public JArray AsJArray()
        {
            _jArray = _jArray ?? JArray.Parse(Body, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });
            return _jArray;
        }

        object _t;

        public T As<T>()
        {
            _t = _t ?? AsJToken().Value<T>();
            return (T)_t;
        }
    }
}
