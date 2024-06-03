# ustc自动化评教脚本
## 前言
考虑到现在评教对教学并无太大意义，且数十门课程的评教所需大量时间，遂制作该自动评教脚本
##  操作指南
### 1.环境要求
python 版本3.8及以上

selenium 版本 4.16.0 及以上

selenium库安装脚本

pip install selenium

下载msedgedriver.exe

官方下载教程：https://learn.microsoft.com/zh-cn/microsoft-edge/webdriver-chromium/?tabs=c-sharp#download-microsoft-edge-webdriver

下载网址：https://learn.microsoft.com/zh-cn/microsoft-edge/webdriver-chromium/?tabs=c-sharp#download-microsoft-edge-webdriver

记得选择与电脑edge版本匹配的msedgedriver.exe

下载完成后放置于一文件夹，并记录路径

### 2.使用指南
修改自动评教.py中一下部分改为你的学号密码，及你的下载的msedgedriver的路径后即可直接运行
```
driver = webdriver.Edge(service=Service(r'Your Path\msedgedriver.exe'), options=edge_options) # 将Your Path修改为你的msedgedriver.exe路径
usrbox.send_keys('Your student ID') # 输入学号
pwdbox.send_keys('Your password')  # 输入密码

```
## 填写结果
本自动化脚本填写的选项除对教材的评价为最后一个（非常差），均为第一个选项（value为1的选项），非常好，及非常困难。

## 注意事项
该自动化评教脚本中填写无法填写科学社会研讨课（有填空题），建议先行填写，以及遇到体育课会退出，再次运行即可。

本脚本须在校园网下使用，否则无法使用
