using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class EffectManager
    {

        private IGameObject m_OwningObject;

        public IGameObject OwningObject
        {
            get { return m_OwningObject; }
        }
        

        public EffectManager(IGameObject owningObject)
        {
            m_OwningObject = owningObject;
            AttachedEffects = new Dictionary<uint, Effect>();
        }

        public Dictionary<uint, Effect> AttachedEffects { get; set; }

        public void DetachExpiredEffects()
        {
            List<Effect> toDetach = new List<Effect>();
            foreach (Effect e in AttachedEffects.Values)
            {
                if (e.TimeRemaining <= 0)
                {
                    toDetach.Add(e);
                }
            }

            for (int i = 0; i < toDetach.Count; i++)
            {
                toDetach[i].EffectExpired();
                DetachEffect(toDetach[i], true);
            }
        }

        public bool DetachEffect(Effect e, bool fireScriptDetachEvent = false)
        {
            if (AttachedEffects.Remove(e.TypeHash))
            {
                if (fireScriptDetachEvent)
                {
                    Dictionary<string, object> args = new Dictionary<string, object>();
                    args.Add("Effect", e.TypeHash);
                    GameEvents.FireEvent(GameEventType.EffectDetached, OwningObject, null, args);
                }
                return true;
            }
            return false;
        }

        public bool AttachEffect(IGameObject target, IGameObject instigator, uint kind, bool fireScriptAttachEvent = false)
        {
            Effect e = Effect.GetEffect(kind) as Effect;
            if (e == null)
            {
                // "That effect does not exist."
                return false;
            }

            e.Target = target;
            e.Instigator = instigator;
            if(AttachEffect(e))
            {               
                return true;
            }
            return false;
        }

        public virtual bool CanAttachEffect(uint kind)
        {
            return true;
        }

        public bool AttachEffect(Effect e, bool fireScriptAttachEvent = false)
        {
            if (e.Target == null)
            {
                return false;
            }

            if (e.TypeHash == 0)
            {
                return false;
            }

            if (AttachedEffects.ContainsKey(e.TypeHash))
            {
                return false;
            }

            if (!CanAttachEffect(e.Information.EffectKind))
            {
                return false;
            }

            if (e.EffectStart == 0)
            {
                e.EffectStart = DateTime.Now.ToUniversalTime().Ticks;
            }

            if (e.Information.DurationKind == EffectDurationType.Time)
            {
                e.TimeRemaining = TimeSpan.FromSeconds(e.Information.Duration).Ticks;
            }
            else if (e.TimeRemaining == 0)
            {
                e.TimeRemaining = e.Information.Duration;
            }

            if (e.LastTick == 0 && (e.Information.DurationKind == EffectDurationType.Time))
            {
                e.LastTick = e.EffectStart;
            }
            // Add it to the local effects list on the character object
            AttachedEffects.Add(e.TypeHash, e);

            if (fireScriptAttachEvent)
            {
                Dictionary<string, object> eargs = new Dictionary<string, object>();
                eargs.Add("Effect", e.TypeHash);
                GameEvents.FireEvent(GameEventType.EffectAttached, e.Target, null, eargs);
            }

            return true;
        }


        public bool UpdateEffectTimers(bool tookTurn)
        {
            foreach (Effect e in AttachedEffects.Values)
            {
                if (e.Information.DurationKind == EffectDurationType.Time)
                {
                    long now = DateTime.Now.ToUniversalTime().Ticks;
                    long endEffect = DateTime.MaxValue.ToUniversalTime().Ticks;
                    long duration = TimeSpan.FromSeconds(e.Information.Duration).Ticks;

                    if (e.Information.Duration > 0)
                    {
                        endEffect = e.EffectStart + duration;
                    }

                    // See if we need to calculate / execute ticks for the effect
                    if (e.Information.TickLength > 0)
                    {
                        long tickLen = TimeSpan.FromSeconds(e.Information.TickLength).Ticks;
                        long timeSinceLastTick = now - e.LastTick;
                        int numTicksPassed = (int)Math.Floor((double)timeSinceLastTick / tickLen);
                        int maxTicks = (int)Math.Floor((double)duration / tickLen);

                        if (numTicksPassed > maxTicks)
                        {
                            numTicksPassed = maxTicks;
                        }

                        for (int i = 0; i < numTicksPassed; i++)
                        {
                            e.Tick();
                        }

                        e.LastTick = e.LastTick + (numTicksPassed * tickLen);
                    }

                    e.TimeRemaining = (e.EffectStart + duration) - now;
                }

                else if (e.Information.DurationKind == EffectDurationType.Turns && tookTurn)
                {
                    e.TimeRemaining--;

                    if (e.Information.TickLength > 0)
                    {
                        e.LastTick = e.LastTick + 1; // number of ticks passed
                        while (e.Information.TickLength <= e.LastTick)
                        {
                            e.Tick();
                            e.LastTick--;
                        }
                    }

                }
            }

            return true;
        }

    }
}
