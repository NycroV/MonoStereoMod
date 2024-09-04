using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        // Nycro 9-4-24:
        //
        // I know this file and method is messy
        // It is literally just a duplicate of the source code,
        // but with the sound logic swapped for MonoStereo.
        //
        // If someone is willing to write IL edits so that this abomination
        // doesn't need to be used, please, be my guest.
        // Until that time, this stays.

        public static void On_Projectile_AI_190_NightsEdge(On_Projectile.orig_AI_190_NightsEdge orig, Projectile self)
        {
            if (self.localAI[0] == 0f && self.type == 984)
            {
                /*SoundEffectInstance soundEffectInstance = SoundEngine.PlaySound((SoundStyle?)SoundID.Item60, self.position);
                if (soundEffectInstance != null)
                    soundEffectInstance.Volume *= 0.65f;*/

                var sound = SoundID.Item60 with { Volume = 0.65f };
                SoundEngine.PlaySound(sound, self.position);
            }

            self.localAI[0] += 1f;
            Player player = Main.player[self.owner];
            float num = self.localAI[0] / self.ai[1];
            float num2 = self.ai[0];
            float num3 = self.velocity.ToRotation();
            float num4 = (float)Math.PI * num2 * num + num3 + num2 * (float)Math.PI + player.fullRotation;
            self.rotation = num4;
            float num5 = 0.2f;
            float num6 = 1f;
            switch (self.type)
            {
                case 982:
                    num5 = 0.6f;
                    break;
                case 997:
                    num5 = 0.6f;
                    break;
                case 983:
                    num5 = 1f;
                    num6 = 1.2f;
                    break;
                case 984:
                    num5 = 0.6f;
                    break;
            }

            self.Center = player.RotatedRelativePoint(player.MountedCenter) - self.velocity;
            self.scale = num6 + num * num5;
            if (self.type == 972)
            {
                if (Math.Abs(num2) < 0.2f)
                {
                    self.rotation += (float)Math.PI * 4f * num2 * 10f * num;
                    float num7 = Terraria.Utils.Remap(self.localAI[0], 10f, self.ai[1] - 5f, 0f, 1f);
                    self.position += self.velocity.SafeNormalize(Vector2.Zero) * (45f * num7);
                    self.scale += num7 * 0.4f;
                }

                if (Main.rand.Next(2) == 0)
                {
                    float f = self.rotation + Main.rand.NextFloatDirection() * ((float)Math.PI / 2f) * 0.7f;
                    Vector2 vector = self.Center + f.ToRotationVector2() * 84f * self.scale;
                    if (Main.rand.Next(5) == 0)
                    {
                        Dust dust = Dust.NewDustPerfect(vector, 14, null, 150, default(Color), 1.4f);
                        dust.noLight = (dust.noLightEmittence = true);
                    }

                    if (Main.rand.Next(2) == 0)
                        Dust.NewDustPerfect(vector, 27, new Vector2(player.velocity.X * 0.2f + (float)(player.direction * 3), player.velocity.Y * 0.2f), 100, default(Color), 1.4f).noGravity = true;
                }
            }

            if (self.type == 982)
            {
                float num8 = self.rotation + Main.rand.NextFloatDirection() * ((float)Math.PI / 2f) * 0.7f;
                Vector2 vector2 = self.Center + num8.ToRotationVector2() * 84f * self.scale;
                Vector2 vector3 = (num8 + self.ai[0] * ((float)Math.PI / 2f)).ToRotationVector2();
                if (Main.rand.NextFloat() * 2f < self.Opacity)
                {
                    Dust dust2 = Dust.NewDustPerfect(self.Center + num8.ToRotationVector2() * (Main.rand.NextFloat() * 80f * self.scale + 20f * self.scale), 278, vector3 * 1f, 100, Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat() * 0.3f), 0.4f);
                    dust2.fadeIn = 0.4f + Main.rand.NextFloat() * 0.15f;
                    dust2.noGravity = true;
                }

                if (Main.rand.NextFloat() * 1.5f < self.Opacity)
                    Dust.NewDustPerfect(vector2, 43, vector3 * 1f, 100, Color.White * self.Opacity, 1.2f * self.Opacity);
            }

            if (self.type == 997)
            {
                float num9 = self.rotation + Main.rand.NextFloatDirection() * ((float)Math.PI / 2f) * 0.7f;
                _ = self.Center + num9.ToRotationVector2() * 84f * self.scale;
                Vector2 vector4 = (num9 + self.ai[0] * ((float)Math.PI / 2f)).ToRotationVector2();
                if (Main.rand.NextFloat() * 2f < self.Opacity)
                {
                    Dust dust3 = Dust.NewDustPerfect(self.Center + num9.ToRotationVector2() * (Main.rand.NextFloat() * 80f * self.scale + 20f * self.scale), 6, vector4 * 4f, 0, default(Color), 0.4f);
                    dust3.noGravity = true;
                    dust3.scale = 1.4f;
                }
            }

            if (self.type == 983)
            {
                float num10 = self.rotation + Main.rand.NextFloatDirection() * ((float)Math.PI / 2f) * 0.7f;
                Vector2 vector5 = self.Center + num10.ToRotationVector2() * 84f * self.scale;
                Vector2 vector6 = (num10 + self.ai[0] * ((float)Math.PI / 2f)).ToRotationVector2();
                if (Main.rand.NextFloat() < self.Opacity)
                {
                    Dust dust4 = Dust.NewDustPerfect(self.Center + num10.ToRotationVector2() * (Main.rand.NextFloat() * 80f * self.scale + 20f * self.scale), 278, vector6 * 1f, 100, Color.Lerp(Color.HotPink, Color.White, Main.rand.NextFloat() * 0.3f), 0.4f);
                    dust4.fadeIn = 0.4f + Main.rand.NextFloat() * 0.15f;
                    dust4.noGravity = true;
                }

                if (Main.rand.NextFloat() * 1.5f < self.Opacity)
                    Dust.NewDustPerfect(vector5, 43, vector6 * 1f, 100, Color.White * self.Opacity, 1.2f * self.Opacity);
            }

            if (self.type == 984)
            {
                float num11 = self.rotation + Main.rand.NextFloatDirection() * ((float)Math.PI / 2f) * 0.7f;
                Vector2 vector7 = self.Center + num11.ToRotationVector2() * 85f * self.scale;
                Vector2 vector8 = (num11 + self.ai[0] * ((float)Math.PI / 2f)).ToRotationVector2();
                Color value = new Color(64, 220, 96);
                Color value2 = new Color(15, 84, 125);
                Lighting.AddLight(self.Center, value2.ToVector3());
                if (Main.rand.NextFloat() * 2f < self.Opacity)
                {
                    Color value3 = Color.Lerp(value2, value, Terraria.Utils.Remap(num, 0f, 0.6f, 0f, 1f));
                    value3 = Color.Lerp(value3, Color.White, Terraria.Utils.Remap(num, 0.6f, 0.8f, 0f, 0.5f));
                    Dust dust5 = Dust.NewDustPerfect(self.Center + num11.ToRotationVector2() * (Main.rand.NextFloat() * 80f * self.scale + 20f * self.scale), 278, vector8 * 1f, 100, Color.Lerp(value3, Color.White, Main.rand.NextFloat() * 0.3f), 0.4f);
                    dust5.fadeIn = 0.4f + Main.rand.NextFloat() * 0.15f;
                    dust5.noGravity = true;
                }

                if (Main.rand.NextFloat() < self.Opacity)
                {
                    Color.Lerp(Color.Lerp(Color.Lerp(value2, value, Terraria.Utils.Remap(num, 0f, 0.6f, 0f, 1f)), Color.White, Terraria.Utils.Remap(num, 0.6f, 0.8f, 0f, 0.5f)), Color.White, Main.rand.NextFloat() * 0.3f);
                    Dust dust6 = Dust.NewDustPerfect(vector7, 107, vector8 * 3f, 100, default(Color) * self.Opacity, 0.8f * self.Opacity);
                    dust6.velocity += player.velocity * 0.1f;
                    dust6.velocity += new Vector2(player.direction, 0f);
                    dust6.position -= dust6.velocity * 6f;
                }
            }

            self.scale *= self.ai[2];
            if (self.localAI[0] >= self.ai[1])
                self.Kill();
        }

        public static void On_Projectile_AI_188_LightsBane(On_Projectile.orig_AI_188_LightsBane orig, Projectile self)
        {
            if (self.soundDelay == 0)
            {
                self.soundDelay = -1;

                /*SoundEffectInstance soundEffectInstance = SoundEngine.PlaySound((SoundStyle?)SoundID.Item60, self.Center);
                if (soundEffectInstance != null)
                    soundEffectInstance.Volume *= 0.15f * self.ai[0];*/

                SoundStyle sound = SoundID.Item60 with { Volume = 0.15f * self.ai[0] };
                SoundEngine.PlaySound(sound, self.Center);
            }

            self.scale = self.ai[0];
            self.localAI[0] += 1f;
            if (++self.frameCounter >= 3)
            {
                self.frameCounter = 0;
                if (++self.frame >= 12)
                {
                    self.Kill();
                    return;
                }
            }

            self.rotation = self.velocity.ToRotation();
            float f = self.rotation;
            float num = 46f * self.scale;
            Vector2 vector = f.ToRotationVector2();
            float num2 = self.localAI[0] / 36f * 4f;
            if (num2 >= 0f && num2 <= 1f)
            {
                Dust dust = Dust.NewDustPerfect(Vector2.Lerp(self.Center - vector * num, self.Center + vector * num, self.localAI[0] / 36f), 278, vector.RotatedBy((float)Math.PI * 2f * Main.rand.NextFloatDirection() * 0.02f) * 8f * Main.rand.NextFloat(), 0, new Color(60, 0, 150), 0.7f * num2);
                dust.noGravity = true;
                dust.noLight = (dust.noLightEmittence = true);
            }
        }
    }
}
