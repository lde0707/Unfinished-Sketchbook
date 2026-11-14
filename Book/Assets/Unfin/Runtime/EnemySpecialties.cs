using System;
using UnityEngine;
namespace Unfin
{
    // One authored strength per species. Phase durations drive art, movement, damage and effects together.
    public enum EnemyGuard { None, Front, Shell, Burrow }
    public enum EnemyLane { Aim, Horizontal, Vertical }
    public sealed class EnemySpecialty
    {
        public string strength,counter;
        public float windup,active,recovery,cooldown,speed,awareness,reach,shotRange;
        public EnemyGuard guard;public EnemyLane lane;public bool lowSweep,radial,retreat;
        public EnemySpecialty(string strength,string counter,float windup,float active,float recovery,float cooldown,float speed=.8f,float awareness=4.5f,float reach=0,float shotRange=7.5f,EnemyGuard guard=EnemyGuard.None,EnemyLane lane=EnemyLane.Aim,bool low=false,bool radial=false,bool retreat=false)
        {this.strength=strength;this.counter=counter;this.windup=windup;this.active=active;this.recovery=recovery;this.cooldown=cooldown;this.speed=speed;this.awareness=awareness;this.reach=reach;this.shotRange=shotRange;this.guard=guard;this.lane=lane;lowSweep=low;this.radial=radial;this.retreat=retreat;}
    }
    public static class EnemySpecialties
    {
        public static EnemySpecialty For(string id)
        {
            switch(id){
            case "COMMON01":return new EnemySpecialty("긴 지상 돌진","점프로 넘긴 뒤 추격",.65f,.38f,.7f,.45f,lane:EnemyLane.Horizontal);
            case "COMMON02":return new EnemySpecialty("현재 높이를 노리는 급강하","날개를 접은 뒤 옆으로 피하기",.7f,.3f,.8f,.5f,awareness:6);
            case "COMMON03":return new EnemySpecialty("착탄 후 잉크 지대","착탄 지점에서 벗어나 점프",.8f,.15f,1f,.65f,shotRange:8);
            case "R101":return new EnemySpecialty("끊임없는 근거리 압박","들어 올린 머리를 보고 후퇴",.5f,.18f,.55f,.25f,1.35f,5,.15f);
            case "R102":return new EnemySpecialty("수평 통로 속사","점프로 넘기거나 탄 베기",.7f,.12f,.7f,.35f,shotRange:9);
            case "R103":return new EnemySpecialty("앞판 방어","뒤쪽 또는 밀치기 후 노출 공략",.85f,.28f,1.05f,.45f,.65f,5,.25f,guard:EnemyGuard.Front);
            case "A01":return new EnemySpecialty("빠른 근접 물기","짧은 사거리 바깥에서 유도",.32f,.13f,.7f,.4f,.5f,3,.25f);
            case "A02":return new EnemySpecialty("머리 위 도약","도약 아래로 지나가 착지 뒤 공격",.75f,.48f,.85f,.5f);
            case "A03":return new EnemySpecialty("접힌 책장 전방위 방어","페이지를 펼친 뒤 공격",.9f,.24f,1.25f,.35f,.3f,3,1.15f,guard:EnemyGuard.Shell);
            case "B01":return new EnemySpecialty("흑연 속 잠복 접근","흙더미를 피하고 솟은 뒤 공격",.7f,.25f,1.15f,.5f,1.6f,6,.25f,guard:EnemyGuard.Burrow);
            case "B02":return new EnemySpecialty("앞판으로 낮게 밀어내기","점프로 뒤쪽 공략",.8f,.3f,.9f,.5f,.7f,5,.4f,guard:EnemyGuard.Front,low:true);
            case "B03":return new EnemySpecialty("머리 위 수직 낙하","아래를 빠르게 통과",.9f,.32f,1.2f,.6f,lane:EnemyLane.Vertical);
            case "C01":return new EnemySpecialty("매달린 통로 봉쇄","거미가 올라가는 틈에 통과",.8f,.55f,.95f,.4f,lane:EnemyLane.Vertical);
            case "C02":return new EnemySpecialty("먼 거리 도약 접근","도약 방향 고정 뒤 반대편으로",.65f,.4f,.8f,.4f,1.1f,6);
            case "C03":return new EnemySpecialty("일정 높이 활공","비행 높이 위아래로 피하기",.8f,.45f,.9f,.45f,lane:EnemyLane.Horizontal);
            case "D01":return new EnemySpecialty("빠른 장거리 추격","멈춰 무는 순간 점프로 넘기기",.55f,.2f,.65f,.25f,2.25f,8,.3f);
            case "D02":return new EnemySpecialty("튀어 오르는 시간차 씨앗","첫 탄을 베거나 두 번째 궤도 피하기",.8f,.15f,.85f,.45f,shotRange:7);
            case "D03":return new EnemySpecialty("넓은 지면 쓸기","손이 닿는 낮은 범위를 점프로 넘기기",.85f,.38f,1.1f,.5f,reach:2f,low:true);
            case "R201":return new EnemySpecialty("두 번 이어지는 창 찌르기","첫 찌르기 후 바로 들어가지 않기",.65f,.48f,.8f,.4f,1.05f,5,.85f);
            case "R202":return new EnemySpecialty("거리 유지와 곡사 둔화탄","곡사 안쪽으로 파고들기",.8f,.15f,.9f,.5f,1.25f,7,shotRange:8,retreat:true);
            case "R203":return new EnemySpecialty("높은 곳에서 긴 급강하","고정된 대각선 밖으로 이동",.8f,.32f,.95f,.45f,awareness:7);
            case "E01":return new EnemySpecialty("등짐을 앞세운 돌진","정면 연타 대신 돌진을 넘기기",.9f,.36f,1.1f,.5f,.65f,6,guard:EnemyGuard.Front,lane:EnemyLane.Horizontal);
            case "E02":return new EnemySpecialty("뒤쪽 밀랍 띠로 추격 차단","앞쪽으로 돌아 공격",.65f,.2f,.7f,.5f,.55f,5,shotRange:4);
            case "E03":return new EnemySpecialty("긴 통로를 빠르게 횡단","횡단 높이를 피하고 복귀 틈 공략",.85f,.38f,.95f,.5f,lane:EnemyLane.Horizontal);
            case "F01":return new EnemySpecialty("양옆 지면 내려치기","바닥 충격을 점프로 피하기",.75f,.22f,.95f,.45f,.8f,5,1.1f,low:true,radial:true);
            case "F02":return new EnemySpecialty("큰 주걱 쪽 방어와 쓸기","작은 집게 쪽으로 돌아가기",.9f,.28f,1f,.45f,.6f,5,.75f,guard:EnemyGuard.Front);
            case "F03":return new EnemySpecialty("양옆 동시 밀랍 분출","중앙 가까운 틈 또는 바깥으로",1f,.2f,1.1f,.55f,shotRange:5);
            case "G01":return new EnemySpecialty("높이가 다른 선반 추격","목표 선반 착지 위치에서 벗어나기",.8f,.45f,.9f,.45f,1f,6);
            case "G02":return new EnemySpecialty("밀치는 동안 유지되는 방패","방패를 벌린 회복 때 공격",.9f,.28f,1.2f,.35f,.65f,5,.35f,guard:EnemyGuard.Front);
            case "G03":return new EnemySpecialty("빠른 장거리 정밀탄","조준 후 점프 또는 패링",.9f,.12f,1f,.6f,shotRange:11);
            case "H01":return new EnemySpecialty("엇박자 장거리 창","긴 준비에 속지 말고 찌른 뒤 접근",1.1f,.14f,.9f,.45f,.6f,5,2f);
            case "H02":return new EnemySpecialty("방패를 때리면 예고 반격","등 뒤 또는 반격 후 공략",.65f,.22f,.95f,.5f,.65f,5,.3f,guard:EnemyGuard.Front);
            case "H03":return new EnemySpecialty("간격을 두고 오는 봉인 탄 두 발","한 발 뒤 멈추지 말고 두 발 모두 피하기",.85f,.5f,.9f,.5f,shotRange:9);
            default:throw new ArgumentException("Missing enemy specialty: "+id);
            }
        }
        public static EnemyArtSource Apply(EnemyArtSource original,EnemySpecialty p)
        {
            var copy=JsonUtility.FromJson<EnemyArtSource>(JsonUtility.ToJson(original));
            var clip=copy.clips[1];var old=clip.motionDurations??clip.durations;
            float a=old[0]+old[1],b=a+old[2],c=b+old[3];
            // Actual seconds: normal playback is 1.12x. Re-time frame endpoints and hit windows identically.
            float x=p.windup*1.12f,y=p.active*1.12f,z=p.recovery*1.12f;
            float Map(float t)=>t<=a?t/a*x:t<=b?x+(t-a)/(b-a)*y:x+y+(t-b)/(c-b)*z;
            float time=0,previous=0;
            for(int i=0;i<clip.durations.Length;i++){time+=clip.durations[i];float end=Map(time);clip.durations[i]=end-previous;previous=end;}
            clip.motionDurations=new[]{x*.55f,x*.45f,y,z};
            if(copy.attackWindows!=null)for(int i=0;i<copy.attackWindows.Length;i++)copy.attackWindows[i]=Map(copy.attackWindows[i]);
            copy.meleeReach=p.reach;return copy;
        }
    }
}
