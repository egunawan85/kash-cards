import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';

const BG = '#05070a';
const CYAN = '#00e6ff';
const INK = '#eef3f8';
const FONT = 'Sora, system-ui, -apple-system, sans-serif';

export const QryptoPromo: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();

  // wordmark entrance
  const markIn = spring({ frame, fps, config: { damping: 200 }, durationInFrames: 30 });
  const markScale = interpolate(markIn, [0, 1], [0.86, 1]);
  const markOpacity = interpolate(frame, [0, 18], [0, 1], { extrapolateRight: 'clamp' });

  // tagline rise
  const tagIn = spring({ frame: frame - 24, fps, config: { damping: 200 }, durationInFrames: 30 });
  const tagY = interpolate(tagIn, [0, 1], [40, 0]);
  const tagOpacity = interpolate(frame, [24, 44], [0, 1], { extrapolateRight: 'clamp' });

  // underline sweep
  const lineW = interpolate(frame, [40, 75], [0, 420], { extrapolateRight: 'clamp', extrapolateLeft: 'clamp' });

  // footer fee line
  const feeOpacity = interpolate(frame, [70, 90], [0, 1], { extrapolateRight: 'clamp' });

  // slow glow pulse
  const glow = 0.5 + 0.5 * Math.sin(frame / 12);

  return (
    <AbsoluteFill style={{ backgroundColor: BG, fontFamily: FONT }}>
      {/* atmosphere: radial cyan glow */}
      <AbsoluteFill
        style={{
          background: `radial-gradient(60% 55% at 78% 12%, rgba(0,230,255,${0.16 + glow * 0.08}), transparent 60%), radial-gradient(55% 50% at 10% 95%, rgba(124,139,255,0.10), transparent 60%)`,
        }}
      />
      {/* faint grid */}
      <AbsoluteFill
        style={{
          backgroundImage:
            'linear-gradient(rgba(255,255,255,0.04) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.04) 1px, transparent 1px)',
          backgroundSize: '64px 64px',
          maskImage: 'radial-gradient(120% 90% at 50% 0%, #000 30%, transparent 80%)',
          WebkitMaskImage: 'radial-gradient(120% 90% at 50% 0%, #000 30%, transparent 80%)',
        }}
      />

      <AbsoluteFill style={{ justifyContent: 'center', alignItems: 'center' }}>
        <div style={{ textAlign: 'center' }}>
          <div
            style={{
              fontSize: 180,
              fontWeight: 800,
              letterSpacing: '-0.04em',
              color: INK,
              opacity: markOpacity,
              transform: `scale(${markScale})`,
              textShadow: `0 0 ${40 + glow * 30}px rgba(0,230,255,${0.25 + glow * 0.2})`,
            }}
          >
            Q<span style={{ color: CYAN }}>rypto</span>
          </div>

          {/* underline */}
          <div
            style={{
              height: 4,
              width: lineW,
              margin: '6px auto 0',
              borderRadius: 4,
              background: `linear-gradient(90deg, transparent, ${CYAN}, transparent)`,
              boxShadow: `0 0 18px ${CYAN}`,
            }}
          />

          <div
            style={{
              marginTop: 44,
              fontSize: 52,
              fontWeight: 600,
              letterSpacing: '-0.02em',
              color: INK,
              opacity: tagOpacity,
              transform: `translateY(${tagY}px)`,
            }}
          >
            Top up with USDT. Spend like <span style={{ color: CYAN }}>cash.</span>
          </div>

          <div
            style={{
              marginTop: 26,
              fontSize: 26,
              fontFamily: 'JetBrains Mono, monospace',
              letterSpacing: '0.04em',
              color: '#aab6c4',
              opacity: feeOpacity,
            }}
          >
            Virtual card &middot; Instant conversion &middot; Flat 3% top-up fee
          </div>
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
