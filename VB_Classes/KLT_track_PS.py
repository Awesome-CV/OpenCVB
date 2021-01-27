'''
Lucas-Kanade tracker
====================

Lucas-Kanade sparse optical flow demo. Uses goodFeaturesToTrack
for track initialization and back-tracking for match verification
between frames.

https://github.com/opencv/opencv/blob/master/samples/python/lk_track.py

Usage
-----
lk_track.py

'''
import numpy as np
import cv2 as cv
import sys
title_window = 'KLT_track_PS.py'

from common import anorm2, draw_str

lk_params = dict( winSize  = (15, 15),
                  maxLevel = 2,
                  criteria = (cv.TERM_CRITERIA_EPS | cv.TERM_CRITERIA_COUNT, 10, 0.03))

feature_params = dict( maxCorners = 500,
                       qualityLevel = 0.3,
                       minDistance = 7,
                       blockSize = 7 )

class App:
    def Open(self):
        self.track_len = 10
        self.detect_interval = 5
        self.tracks = []
        self.frame_idx = 0
        print(__doc__)
        from PyStream import PyStreamRun
        PyStreamRun(self.OpenCVCode, 'lk_track_PS.py')

    def OpenCVCode(self, frame, depth_colormap, frameCount):
        frame_gray = cv.cvtColor(frame, cv.COLOR_BGR2GRAY)
        vis = frame.copy()

        if len(self.tracks) > 0:
            img0, img1 = self.prev_gray, frame_gray
            p0 = np.float32([tr[-1] for tr in self.tracks]).reshape(-1, 1, 2)
            p1, _st, _err = cv.calcOpticalFlowPyrLK(img0, img1, p0, None, **lk_params)
            p0r, _st, _err = cv.calcOpticalFlowPyrLK(img1, img0, p1, None, **lk_params)
            d = abs(p0-p0r).reshape(-1, 2).max(-1)
            good = d < 1
            new_tracks = []
            for tr, (x, y), good_flag in zip(self.tracks, p1.reshape(-1, 2), good):
                if not good_flag:
                    continue
                tr.append((x, y))
                if len(tr) > self.track_len:
                    del tr[0]
                new_tracks.append(tr)
                cv.circle(vis, (x, y), 2, (0, 255, 0), -1)
            self.tracks = new_tracks
            cv.polylines(vis, [np.int32(tr) for tr in self.tracks], False, (0, 255, 0))
            draw_str(vis, (20, 20), 'track count: %d' % len(self.tracks))

        if self.frame_idx % self.detect_interval == 0:
            mask = np.zeros_like(frame_gray)
            mask[:] = 255
            for x, y in [np.int32(tr[-1]) for tr in self.tracks]:
                cv.circle(mask, (x, y), 5, 0, -1)
            p = cv.goodFeaturesToTrack(frame_gray, mask = mask, **feature_params)
            if p is not None:
                for x, y in np.float32(p).reshape(-1, 2):
                    self.tracks.append([(x, y)])


        self.frame_idx += 1
        self.prev_gray = frame_gray
        cv.imshow('KLT_track_PS', vis)

App().Open()