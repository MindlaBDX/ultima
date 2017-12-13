from django.conf.urls import url

from . import views

urlpatterns = [
    url(r'^$', views.index, name='index'),
    url(r'^propose/$', views.propose, name='propose'),
    url(r'^what_has_been_proposed/$', views.what_has_been_proposed, name='what_has_been_proposed'),
    url(r'^acceptation/$', views.acceptation, name='acceptation'),
    url(r'^accepted/$', views.accepted, name='accepted'),

]
