using MonoStereo;
using ReLogic.Content.Sources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.AudioSytem
{
    public class MonoStereoAudioSystem : IAudioSystem
    {
        public static void Initialize()
        {
            AudioManager.Initialize(() => !MonoStereoMod.ModRunning || Main.instance is null);
        }

        public void Dispose()
        {
            
        }

        public bool IsTrackPlaying(int trackIndex)
        {
            throw new NotImplementedException();
        }

        public void LoadCue(int cueIndex, string cueName)
        {
            
        }

        public void LoadFromSources()
        {
            
        }

        public void PauseAll()
        {
            
        }

        public IEnumerator PrepareWaveBank()
        {
            throw new NotImplementedException();
        }

        public void ResumeAll()
        {
            
        }

        public void Update()
        {
            
        }

        public void UpdateAmbientCueState(int i, bool gameIsActive, ref float trackVolume, float systemVolume)
        {
            
        }

        public void UpdateAmbientCueTowardStopping(int i, float stoppingSpeed, ref float trackVolume, float systemVolume)
        {
            
        }

        public void UpdateAudioEngine()
        {
            
        }

        public void UpdateCommonTrack(bool active, int i, float totalVolume, ref float tempFade)
        {
            
        }

        public void UpdateCommonTrackTowardStopping(int i, float totalVolume, ref float tempFade, bool isMainTrackAudible)
        {
            
        }

        public void UpdateMisc()
        {
            
        }

        public void UseSources(List<IContentSource> sources)
        {
            
        }
    }
}
