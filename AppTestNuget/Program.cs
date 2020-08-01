using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Love;
using Spine;

namespace example_x
{
    class Program : Scene
    {
        const string assetsFolder = "example_data/";

        public class Pack
        {
            readonly public Skeleton skeleton;
            readonly public AnimationState state;

            public Pack(Skeleton skeleton, AnimationState state)
            {
                this.skeleton = skeleton;
                this.state = state;
            }
        }


        List<Pack> list = new List<Pack>();
        int activeSkeleton = 0;
        SwirlEffect swirl = new SwirlEffect(400);
        float swirlTime = 0;
        SkeletonRenderer skeletonRenderer = new SkeletonRenderer();


        public Pack LoadSkeleton(string jsonFile, string atlasFile, string animation, string skin, float scale, float x, float y)
        {
            Atlas atlas = new Atlas(assetsFolder + atlasFile + ".atlas", new LoveTextureLoader());
            var json = new SkeletonJson(atlas);
            json.Scale = scale;
            var skeletonData = json.ReadSkeletonData(assetsFolder + jsonFile + ".json");
            var skeleton = new Skeleton(skeletonData);
            skeleton.X = x;
            skeleton.Y = y;
            skeleton.ScaleY = -1;
            if (skin != null)
            {
                skeleton.SetSkin(skin);
            }
            skeleton.SetToSetupPose();
            var stateData = new AnimationStateData(skeletonData);
            var state = new AnimationState(stateData);


            state.SetAnimation(0, animation, true);
            if (jsonFile == "spineboy-ess")
            {
                stateData.SetMix("walk", "jump", 0.5f);
                stateData.SetMix("jump", "run", 0.5f);
                state.AddAnimation(0, "jump", false, 3);
                state.AddAnimation(0, "run", true, 0);
            }

            if (jsonFile == "raptor-pro")
            {
                swirl.CenterY = -200;
                //skeleton.VertexEffect = swirl;
            }

            if (jsonFile == "mix-and-match-pro")
            {
                // Create a new skin, by mixing and matching other skins
                // that fit together. Items making up the girl are individual
                // skins. Using the skin API, a new skin is created which is
                // a combination of all these individual item skins.
                var lskin = new Skin("mix-and-match");
                lskin.AddSkin(skeletonData.FindSkin("skin-base"));
                lskin.AddSkin(skeletonData.FindSkin("nose/short"));
                lskin.AddSkin(skeletonData.FindSkin("eyelids/girly"));
                lskin.AddSkin(skeletonData.FindSkin("eyes/violet"));
                lskin.AddSkin(skeletonData.FindSkin("hair/brown"));
                lskin.AddSkin(skeletonData.FindSkin("clothes/hoodie-orange"));
                lskin.AddSkin(skeletonData.FindSkin("legs/pants-jeans"));
                lskin.AddSkin(skeletonData.FindSkin("accessories/bag"));
                lskin.AddSkin(skeletonData.FindSkin("accessories/hat-red-yellow"));
                skeleton.SetSkin(lskin);
            }

            // set some event callbacks
            state.Start += (entry) => Console.WriteLine(entry.TrackIndex + " start: " + entry.Animation.Name);
            state.Interrupt += (entry) => Console.WriteLine(entry.TrackIndex + " interrupt: " + entry.Animation.Name);
            state.End += (entry) => Console.WriteLine(entry.TrackIndex + " end: " + entry.Animation.Name);
            state.Complete += (entry) => Console.WriteLine(entry.TrackIndex + " complete: " + entry.Animation.Name);
            state.Dispose += (entry) => Console.WriteLine(entry.TrackIndex + " dispose: " + entry.Animation.Name);
            state.Event += (entry, e) => Console.WriteLine(
                entry.TrackIndex +
                " event: " +
                entry.Animation.Name +
                ", " +
                e.Data.Name +
                ", " +
                e.Int +
                ", " +
                e.Float +
                ", '" +
               e.String +
                "'" +
                ", " +
                e.Volume +
                ", " +
                e.Balance
                );

            state.Update(0.5f);
            state.Apply(skeleton);

            return new Pack(skeleton, state);
        }

        public override void Load()
        {
            Lua.Init();

            list.Add(LoadSkeleton("mix-and-match-pro", "mix-and-match", "dance", null, 0.5f, 400, 500));
            list.Add(LoadSkeleton("spineboy-pro", "spineboy", "walk", null, 0.5f, 400, 500));
            list.Add(LoadSkeleton("stretchyman-pro", "stretchyman", "sneak", null, 0.5f, 200, 500));
            list.Add(LoadSkeleton("coin-pro", "coin", "animation", null, 0.5f, 400, 300));
            list.Add(LoadSkeleton("raptor-pro", "raptor", "walk", null, 0.3f, 400, 500));
            list.Add(LoadSkeleton("goblins-pro", "goblins", "walk", "goblin", 1, 400, 500));
            list.Add(LoadSkeleton("tank-pro", "tank", "drive", null, 0.2f, 600, 500));
            list.Add(LoadSkeleton("vine-pro", "vine", "grow", null, 0.3f, 400, 500));
        }


        public override void Update(float dt)
        {
            // Update the state with the delta time, apply it, and update the world transforms.
            var state = list[activeSkeleton].state;
            var skeleton = list[activeSkeleton].skeleton;
            state.Update(dt);
            state.Apply(skeleton);
            skeleton.UpdateWorldTransform();

            //// vertex effect
            //skeletonRenderer.VertexEffect = swirl;
            //if (skeletonRenderer.VertexEffect != null)
            //{
            //    swirlTime = swirlTime + dt;
            //    var percent = swirlTime % 2;
            //    if (percent > 1) percent = 1 - (percent - 1);
            //    swirl.Angle = InterpolationApply(InterpolationPow2, -60f, 60f, percent);
            //}
            Window.SetTitle("fps : " + FPSCounter.GetFPS() + "  draw-call : " + skeletonRenderer.DrawCallCount);
        }


        static float InterpolationApply(Func<float, float> func, float start, float _end, float a)
        {
            return start + (_end - start) * func(a);
        }

        static float InterpolationPow2(float a)
        {
            if (a <= 0.5) return Mathf.Pow(a * 2, 2) / 2;
            return Mathf.Pow((a - 1) * 2, 2) / -2 + 1;
        }

        bool isShowDebugView = false;
        public override void Draw()
        {
            Graphics.SetColor(Color.White);
            var skeleton = list[activeSkeleton].skeleton;
            skeletonRenderer.Draw(skeleton);

            Graphics.Print("Press [space] to toggle debug view\nPress mouse button to change skeleton");
            if (Keyboard.IsPressed(KeyConstant.Space))
                isShowDebugView = !isShowDebugView;
            if (isShowDebugView)
                new SkeletonDebugRenderer().Draw(skeleton);
        }

        public override void MousePressed(float x, float y, int button, bool isTouch)
        {
            activeSkeleton = activeSkeleton + 1;
            if (activeSkeleton >= list.Count)
                activeSkeleton = 0;
        }


        static void Main(string[] args)
        {
            Boot.Init();
            Boot.Run(new Program());
        }
    }
}
