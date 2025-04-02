extern alias SC;

using SC::SharedComponents.Py;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectChatWindow : DirectWindow
    {
        #region Fields

        private int? _memberCount;
        private List<DirectCharacter> _members;
        private List<DirectChatMessage> _messages;

        #endregion Fields

        #region Constructors

        public DirectChatWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
            var id = pyWindow.Call("GetChannelId");

            if (id.GetPyType() == PyType.TupleType)
                ChannelId = (string)id.GetItemAt(0).GetItemAt(0);
            if (id.GetPyType() == PyType.StringType)
                ChannelId = (string)id;
            if (id.GetPyType() == PyType.IntType)
                ChannelId = ((long)id).ToString();
            if (id.GetPyType() == PyType.UnicodeType)
                ChannelId = id.ToUnicodeString();

            ShowUserList = (bool)pyWindow.Attribute("_show_member_list_setting").Call("is_enabled");
            ChatChannelCategory = PyWindow.Attribute("channelCategory").ToUnicodeString();
        }

        #endregion Constructors

        #region Properties

        public string ChannelId { get; private set; }
        public string ChatChannelCategory { get; private set; }

        public int MemberCount
        {
            get
            {
                if (_memberCount == null)
                    _memberCount = PyWindow.Attribute("members").ToDictionary().Count();

                return (int)_memberCount;
            }
        }

        public List<DirectCharacter> Members
        {
            get
            {
                if (_members == null)
                {
                    _members = new List<DirectCharacter>();

                    // Only do this if user list is shown
                    if (ShowUserList)
                    {
                        var members = PyWindow.Attribute("members").ToDictionary();

                        //0 - corpid
                        //1 - role
                        //2 - warfactionid
                        //3 - allianceid
                        //4 - typeid

                        foreach (var member in members)
                        {
                            var character = new DirectCharacter(DirectEve);
                            character.CharacterId = (long)member.Key;
                            var listAttr = member.Value.ToDictionary();
                            if (listAttr.Count > 3)
                            {
                                character.CorporationId = (long)listAttr.ElementAt(0).Value;
                                character.AllianceId = (long)listAttr.ElementAt(3).Value;
                                character.WarFactionId = (long)listAttr.ElementAt(2).Value;
                                _members.Add(character);
                            }
                            else
                            {
                                DirectEve.Log($"Warning: listAttr.Count <= 3.   CharId {character.CharacterId} ");
                                //DirectEve.Log($"PyObjDump: {member.Value.LogObject()}");
                            }
                        }
                    }
                }

                return _members;
            }
        }

        public List<DirectChatMessage> Messages
        {
            get
            {
                if (_messages == null)
                    _messages = PyWindow.Attribute("messages").ToList().Select(m => new DirectChatMessage(DirectEve, m)).ToList();

                return _messages;
            }
        }

        public bool ShowUserList { get; private set; }

        #endregion Properties

        #region Methods

        public bool Speak(string message)
        {
            PyWindow.Attribute("input").Call("SetValue", message);
            return DirectEve.ThreadedCall(PyWindow.Attribute("InputKeyUp"));
        }

        #endregion Methods
    }
}