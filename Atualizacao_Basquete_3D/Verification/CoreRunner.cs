using System;
using MarbleBasket3D.Core;
class CoreRunner
{
    private static int checks;
    static void Check(bool value, string description) { checks++; if (!value) throw new Exception(description); }
    static void Main()
    {
        var match = new MatchRules3D(); match.Reset(90, 3);
        Check(!match.Score(TeamId.Red), "ready cannot score"); match.Start();
        Check(match.Phase == MatchPhase.Countdown, "countdown");
        for(int i=0;i<299;i++) match.Tick(.01); Check(match.Phase==MatchPhase.Countdown,"full countdown");
        match.Tick(.011); Check(match.Active&&match.Remaining==90,"simultaneous release boundary");
        match.Score(TeamId.Red);match.Pause();double remaining=match.Remaining;match.Tick(30);
        Check(match.Remaining==remaining&&!match.Score(TeamId.Blue),"pause freezes rules");match.Resume();match.Tick(90);
        Check(match.Result.winner==TeamId.Red&&match.Result.redScore==2,"regulation result");
        Check(!match.Score(TeamId.Blue),"buzzer closes scoring");var result=match.Result;match.Tick(10);Check(ReferenceEquals(result,match.Result),"result immutable after finish");
        match.Reset(1,0);Check(match.Red==0&&match.Blue==0&&match.Result==null,"reset state");match.Start();match.Tick(1);
        Check(match.Phase==MatchPhase.SuddenDeath&&match.Result==null,"tied overtime");match.Score(TeamId.Blue);
        Check(match.Result.suddenDeath&&match.Result.winner==TeamId.Blue,"next valid basket wins");
        Check(!match.Score(TeamId.Red),"sudden death ends once");
        {
            var shot = new ShotValidation3D();
            Check(!shot.Sample(0,4,0,0,3.7,0,.495,.18),"no launch means no basket");shot.Launch();
            Check(!shot.Sample(0,3,0,0,3.7,0,.495,.18),"from below cannot arm");
            Check(!shot.Sample(0,4,0,0,3.7,0,.495,.18),"upward no basket");
            Check(shot.Sample(0,3.5,0,0,3.7,0,.495,.18),"descending valid");
            Check(!shot.Sample(0,3.4,0,0,3.7,0,.495,.18),"no duplicate");
            shot.Launch();shot.Sample(0,4,0,0,3.7,0,.495,.18);
            Check(shot.Sample(0,3,0,0,3.7,0,.495,.18),"same ball new attempt scores again");
            shot.Launch();shot.Sample(.55,4,0,0,3.7,0,.495,.18);
            Check(!shot.Sample(.55,3,0,0,3.7,0,.495,.18),"side cannot score");
            shot.Reset(); Check(shot.Attempt==0&&!shot.Armed&&!shot.Scored,"shot reset");
        }
        var swept=new ShotValidation3D();swept.Launch();swept.Sample(-.2,5,0,0,3.7,0,.495,.18);
        Check(swept.Sample(.2,2.4,0,0,3.7,0,.495,.18),"swept crossing avoids tunneling");
        foreach(double angle in new[]{48.0,56,64})
        {
            double d=Math.Sqrt(5.24*5.24+.84*.84),dy=1.85,g=9.81,v=LaunchMath3D.RequiredSpeed(d,dy,angle,g),a=angle*Math.PI/180;
            double t=d/(v*Math.Cos(a));Check(Math.Abs(v*Math.Sin(a)*t-g*t*t*.5-dy)<.000001,"ballistic target");
        }
        foreach(double radius in new[]{.12,.18,.22})
        for(int xi=-24;xi<=24;xi++) for(int zi=-24;zi<=24;zi++)
        {
            double x=xi*.025,z=zi*.025;
            var gate=new ShotValidation3D();gate.Launch();gate.Sample(x,4, z,0,3.7,0,.495,radius);
            bool actual=gate.Sample(x,3,z,0,3.7,0,.495,radius);
            bool expected=x*x+z*z<(.495-radius)*(.495-radius);
            Check(actual==expected,"swept radius clearance grid");
        }
        var partial=new ShotValidation3D();partial.Launch();partial.Sample(0,4.1,0,0,3.7,0,.495,.18);
        Check(!partial.Sample(0,3.65,0,0,3.7,0,.495,.18),"partial entry is not a basket yet");
        Check(partial.Sample(0,3.4,0,0,3.7,0,.495,.18),"whole sphere below confirms basket");
        partial.Launch();partial.Sample(0,4.1,0,0,3.7,0,.495,.18);partial.Sample(0,3.65,0,0,3.7,0,.495,.18);
        Check(!partial.Sample(0,3.8,0,0,3.7,0,.495,.18)&&!partial.Scored,"bounce out cancels partial entry");
        Console.WriteLine(checks+" pure C# rules/scoring/ballistic checks passed.");
    }
}
