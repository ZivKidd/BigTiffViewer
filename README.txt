针对没有tile的bigtiff
打开软件后将tif影像拖入即可使用
默认影像的宽大于高，所以第一次读入时只读最左边一部分以填充满画幅
始终只读入当前展示区域的影像，当缩放和移动时自己计算当前需要的区域

感谢libtiff.net提供的超强读取模块


For bigtiff without tiles
The width of the default image is bigger than the height, so only the leftmost part is read to fill the frame
Always only read the image of the current display area, and calculate the currently needed area when zooming and moving
Thanks to libtiff.net for the great module
