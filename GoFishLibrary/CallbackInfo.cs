/*!	\file		CallbackInfo.cs
	\author		Haohan Liu, Dmytro Liaska
	\date		2019-04-08

  CallbackInfo class implementation
*/

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoFishLibrary
{
    [DataContract]
    public class CallbackInfo
    {
        [DataMember] public int NumCards { get; private set; }
        [DataMember] public List<string> PlayerNames { get; private set; }
        [DataMember] public string GameLogs { get; private set; }
        [DataMember] public int PlayerIdx { get; private set; }
        [DataMember] public bool EmptyShoe { get; private set; }
        [DataMember] public List<string> GameWinners { get; private set; }
        [DataMember] public bool GameOver { get; private set; }

        public CallbackInfo(int c, List<string> pNames, string gameLogs, int pIdx, bool emptyShoe, List<string> gWinners, bool gOver)
        {
            NumCards = c;
            PlayerNames = pNames;
            GameLogs = gameLogs;
            PlayerIdx = pIdx;
            EmptyShoe = emptyShoe;
            GameWinners = gWinners;
            GameOver = gOver;
        }
    }
}
