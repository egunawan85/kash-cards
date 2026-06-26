import { Config } from '@remotion/cli/config';
import { existsSync } from 'fs';

Config.setVideoImageFormat('jpeg');
Config.setOverwriteOutput(true);
Config.setConcurrency(4);

// Local optimization: reuse an already-installed Chromium so renders don't need
// Remotion's own (sometimes flaky) Chrome download. If this path is absent,
// Remotion downloads and manages its own headless shell automatically.
const localChrome =
  '/Users/jarvis/Library/Caches/ms-playwright/chromium_headless_shell-1223/chrome-headless-shell-mac-arm64/chrome-headless-shell';
if (existsSync(localChrome)) {
  Config.setBrowserExecutable(localChrome);
}
