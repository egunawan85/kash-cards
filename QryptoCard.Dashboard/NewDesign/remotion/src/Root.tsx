import { Composition } from 'remotion';
import { QryptoPromo } from './QryptoPromo';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="QryptoPromo"
      component={QryptoPromo}
      durationInFrames={150}
      fps={30}
      width={1920}
      height={1080}
    />
  );
};
