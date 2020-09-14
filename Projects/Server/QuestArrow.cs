using Server.Network;

namespace Server
{
    public class QuestArrow
    {
        public QuestArrow(Mobile m, Mobile t)
        {
            Running = true;
            Mobile = m;
            Target = t;
        }

        public QuestArrow(Mobile m, Mobile t, int x, int y) : this(m, t)
        {
            Update(x, y);
        }

        public Mobile Mobile { get; }

        public Mobile Target { get; }

        public bool Running { get; private set; }

        public void Update()
        {
            Update(Target.X, Target.Y);
        }

        public void Update(int x, int y)
        {
            if (!Running)
            {
                return;
            }

            var ns = Mobile.NetState;

            if (ns == null)
            {
                return;
            }

            if (ns.HighSeas)
            {
                ns.Send(new SetArrowHS(x, y, Target.Serial));
            }
            else
            {
                ns.Send(new SetArrow(x, y));
            }
        }

        public void Stop()
        {
            Stop(Target.X, Target.Y);
        }

        public void Stop(int x, int y)
        {
            if (!Running)
            {
                return;
            }

            Mobile.ClearQuestArrow();

            var ns = Mobile.NetState;

            if (ns != null)
            {
                if (ns.HighSeas)
                {
                    ns.Send(new CancelArrowHS(x, y, Target.Serial));
                }
                else
                {
                    ns.Send(new CancelArrow());
                }
            }

            Running = false;
            OnStop();
        }

        public virtual void OnStop()
        {
        }

        public virtual void OnClick(bool rightClick)
        {
        }
    }
}
