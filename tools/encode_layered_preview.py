"""Encode actual Unity-rendered frames. Does not generate or edit card art."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tmp/video-deps'))
import imageio_ffmpeg

parser = argparse.ArgumentParser()
parser.add_argument('--folder', default='output/layered-cards')
args = parser.parse_args()
folder = (ROOT / args.folder).resolve()
frames = folder / 'frames'
expected = [frames / f'frame-{i:04}.png' for i in range(288)]
assert all(p.is_file() for p in expected), 'Unity render must finish all 288 frames first'
video = folder / 'layered-cards-preview.mp4'
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
subprocess.run([ffmpeg, '-y', '-hide_banner', '-loglevel', 'warning', '-framerate', '24',
    '-i', str(frames / 'frame-%04d.png'), '-frames:v', '288', '-c:v', 'libx264',
    '-preset', 'medium', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(video)], check=True)
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-i', str(video), '-f', 'null', '-'], check=True)
result = dict(file=video.name, width=1600, height=1080, fps=24, frames=288, seconds=12,
              bytes=video.stat().st_size, source='Unity URP frame capture, not generated video')
(folder / 'video-report.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, indent=2))
