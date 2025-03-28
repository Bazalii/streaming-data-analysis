Я выбрал обработку данных по изменению курсов валют на разных биржах с принятием решения о покупке/продаже акций на основании разницы курсов на биржах за 5 секунд.
Помимо этого необходимо уметь строить аналитику по изменению средних курсов валют за определенный период времени.
Для генерации данных будет написан мой генератор, который будет симулировать торговлю на разных биржах – курсы валют будут изменяться раз в секунду и на основании этих изменений будет выполняться решение о покупке-продаже валют.

---

**Какие данные для этой области являются потоковыми?**

Данные об изменении курсов валют.

---

**Какие результаты мы хотим получить в результате обработки?**

Система должна уметь принимать решения о покупке/продаже тех или иных активов на основании полученных данных о курсах валют.
Помимо этого должны собираться данные по изменению курсов валют и должна быть возможность осуществления аналитики изменения курса за определенные периоды времени – минута, час, день, неделя, месяц.

---

**Как в процессе обработки можно задействовать машинное обучение?**

В рамках расширения системы можно обучить модель анализировать тренды и принимать решение о покупке/продаже активов в рамках выбранных стратегий.

---

**Как предметная область относится к запаздыванию обработки? Насколько это критично?**
Запаздывание обработки очень критично для принятия решения о покупке/продаже, но некритично для сбора данных о статистике.

---

**Как предметная область относится к потере данных? Насколько это критично? Какую семантику (не менее одного раза, не более одного раза, ровно один раз) следует выбрать?**

Для принятия решений потеря данных критична, для статистики в целом некритична.
Можно использовать семантику не более одного раза.
