from django.conf.urls import url, include
from django.contrib import admin

from game import views

urlpatterns = [
    url(r'^', include('game.urls')),
    url(r'^admin/', admin.site.urls),
]
