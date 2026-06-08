from selenium import webdriver
from selenium.webdriver.edge.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.edge.options import Options
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from time import sleep
import os

edge_options = Options()
edge_options.add_argument("--no-sandbox")

driver = webdriver.Edge(service=Service(r'Your Path\msedgedriver.exe'), options=edge_options) # 输入你的msedgedriver.exe路径

driver.get('https://tqm.ustc.edu.cn/')


news_link = driver.find_element(By.CLASS_NAME, 'LoginZKDCustomization_btn_wrap-zIeEo')
news_link.click()
usrbox=driver.find_element(By.ID,'username')
usrbox.send_keys('Your student ID') # 输入学号
pwdbox=driver.find_element(By.ID,'password')
pwdbox.send_keys('Your password')  # 输入密码
login_box=driver.find_element(By.ID,'login')
login_box.click()
sleep(3)
tqm_button=driver.find_element(By.XPATH,'/html/body/div[1]/section/section/main/div/main/div[1]/div/div/div[3]/div[1]/div[3]/div/div/div/div/div/div/div/table/tbody/tr/td[7]/span')
tqm_button.click()
# 使用WebDriver等待确保表格加载完成
table_rows = WebDriverWait(driver, 10).until(
    EC.presence_of_all_elements_located((By.XPATH, "//tbody[@class='ant-table-tbody']/tr"))
)

# 遍历表格行并点击评价按钮
for row in table_rows:
    evaluate_buttons = row.find_elements(By.XPATH,".//td[@class='ant-table-row-cell-break-word']/span[text()='评价']")
    for evaluate_button in evaluate_buttons:
        evaluate_button.click()
        break
    break
sleep(2)
while(1):
    # 找到所有单选按钮元素
    radio_buttons = driver.find_elements(By.CLASS_NAME,"ant-radio-input")

    # 选择相应的单选按钮（例如，“非常扎实”）
    desired_value = "1"
    print(len(radio_buttons))
    for radio_button in radio_buttons:
        print(radio_button.get_attribute("value"))
        if radio_button.get_attribute("value") == desired_value:
            radio_button.click()
            print('click')

    send_button=driver.find_element(By.CLASS_NAME,'index_submit-2EYSG')
    send_button.click()
    sleep(6)
    # 找到“确定”按钮并点击
    confirm_button = driver.find_element(By.XPATH,".//button[@class='ant-btn ant-btn-primary']/span[text()='确定']")
    driver.execute_script("arguments[0].click();", confirm_button)
    sleep(2)
    try:
        next_course_button = driver.find_element(By.XPATH,".//button[@class='ant-btn ant-btn-primary']/span[text()='下一门课程']")
    except:
        next_course_button = driver.find_element(By.XPATH,".//button[@class='ant-btn ant-btn-primary']/span[text()='下一位教师']")
    driver.execute_script("arguments[0].click();", next_course_button)
    sleep(2)
sleep(10)
