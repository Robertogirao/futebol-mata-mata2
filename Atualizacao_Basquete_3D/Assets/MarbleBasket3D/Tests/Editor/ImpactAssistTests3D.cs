using NUnit.Framework;
using UnityEngine;

namespace MarbleBasket3D.Tests
{
    public sealed class ImpactAssistTests3D
    {
        [Test] public void LightAssistancePreservesBadImpactAndLimitsVelocityChange()
        {
            var settings=ScriptableObject.CreateInstance<ArenaSettings3D>();
            try
            {
                Vector3 position=new Vector3(.3f,2,-1.5f),physical=new Vector3(-8,5,0);
                for(int i=0;i<120;i++)
                {
                    Vector3 corrected=ImpactAssist3D.Velocity(position,ArenaBuilder3D.Rim,physical,settings,i*.1f);
                    Assert.That(Vector3.Distance(corrected,physical),Is.LessThanOrEqualTo(physical.magnitude*.25f+.0001f));
                    Assert.That(corrected.x,Is.LessThan(0),"Uma pancada para o lado errado não deve virar uma cesta dirigida.");
                }
            }
            finally{Object.DestroyImmediate(settings);}
        }
        [Test] public void DisabledHelpAndGentleContactsPreservePhysicalVelocity()
        {
            var settings=ScriptableObject.CreateInstance<ArenaSettings3D>();
            try
            {
                Vector3 position=new Vector3(.3f,2,-1.5f),physical=new Vector3(3,6,-1);
                settings.impactAssist=false;
                Assert.That(ImpactAssist3D.Velocity(position,ArenaBuilder3D.Rim,physical,settings,1),Is.EqualTo(physical));
                settings.impactAssist=true;settings.impactAssistStrength=0;
                Assert.That(ImpactAssist3D.Velocity(position,ArenaBuilder3D.Rim,physical,settings,1),Is.EqualTo(physical));
                settings.impactAssistStrength=.25f;
                Assert.That(ImpactAssist3D.Velocity(position,ArenaBuilder3D.Rim,Vector3.zero,settings,1),Is.EqualTo(Vector3.zero));
            }
            finally{Object.DestroyImmediate(settings);}
        }
    }
}
