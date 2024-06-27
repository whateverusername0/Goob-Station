using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    // Goobstation - emotes!!
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, EmoteEvent>(OnEmote);
    }

    public void OnEmote(EntityUid uid, BodyComponent comp, ref EmoteEvent args)
    {
        if (args.Emote.ID == "Flip")
        {
            var anim = new Animation()
            {
                Length = TimeSpan.FromSeconds(1),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Rotation),
                        InterpolationMode = Robust.Shared.Animations.AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0),
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), .5f),
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), .5f),
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0),
                        }
                    }
                },
            };

            _anim.Play(uid, anim, "Flip");
        }
    }
}
