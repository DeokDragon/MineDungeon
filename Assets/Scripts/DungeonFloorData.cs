using System;
using UnityEngine;

[CreateAssetMenu(menuName="Mine Dungeon/Dungeon Floor")]
public sealed class DungeonFloorData : ScriptableObject
{
    public string id;
    [Min(1)] public int floorNumber=1;
    public string displayName;
    public Sprite previewImage;
    public bool testConfiguration=true;
    [Serializable] public sealed class Difficulty
    {
        [Min(1)] public int requiredLevel=1;
        public bool implemented;
        public DungeonRoom[] normalRooms;
        public DungeonRoom bossRoom;
        [Min(1)] public int normalRoomCount=3;
        public ChestRewardPreviewData chestPreview;
        public bool IsPlayable
        {
            get
            {
                if(!implemented||normalRoomCount<1||bossRoom==null||normalRooms==null||normalRooms.Length==0)return false;
                if(bossRoom.gameObject.scene.IsValid()||!bossRoom.gameObject.activeSelf)return false;
                foreach(var room in normalRooms)if(room==null||room.gameObject.scene.IsValid()||!room.gameObject.activeSelf)return false;
                return true;
            }
        }
    }
    public Difficulty[] difficulties=new Difficulty[4];
    public Difficulty GetDifficulty(int index)=>difficulties!=null&&index>=0&&index<difficulties.Length?difficulties[index]:null;
    public static string DifficultyName(int index)=>index>=0&&index<4?new[]{"쉬움","보통","어려움","악몽"}[index]:"준비 중";
}
