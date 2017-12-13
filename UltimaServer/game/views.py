from django.http import HttpResponse
from django.views.decorators.csrf import csrf_exempt
from .models import Propositions
from .parameters import group


def compatible_web_gl_response(to_reply):
    """ For a response to be compatible with WebGL app build with Unity, a customized header is necessary"""
    response = HttpResponse(to_reply)
    response["Access-Control-Allow-Credentials"] = "true"
    response["Access-Control-Allow-Headers"] = "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time"
    response["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS"
    response["Access-Control-Allow-Origin"] = "*"
    return response


@csrf_exempt
def index(request):
    """Assign a role to player: Create a table if role is P1, otherwise return game id"""

    # Shape of reply: "<id game>/<role>"

    available_games = Propositions.objects.filter(accessible=True)
    if len(available_games):

        # Assign the role of player 2
        entry = available_games[0]
        _id_ = entry.id
        entry.accessible = False
        entry.save(force_update=True)
        return compatible_web_gl_response(str(_id_) + "/2")

    else:

        # Assign the role of player 1
        entry = Propositions(
            accessible=True, complete=False, proposition=0, answer=None, group=group)
        entry.save()
        _id_ = Propositions.objects.last().id
        return compatible_web_gl_response(str(_id_) + "/1")


@csrf_exempt
def propose(request):
    """Called by first player: give his proposition to second player """

    if "id" in request.POST and "proposition" in request.POST:

        v = request.POST["proposition"]  # Get the value of the slider
        _id_ = request.POST["id"]  # Get the game ID

        # Update table
        entry = Propositions.objects.get(id=_id_)
        entry.proposition = v
        entry.complete = True
        entry.save(force_update=True)

        return compatible_web_gl_response("ok")

    else:
        return compatible_web_gl_response("error")


@csrf_exempt
def what_has_been_proposed(request):
    """Called by second player: ask the proposition from first player"""

    if 'id' in request.POST:
        _id_ = request.POST['id']  # Get the game ID

        complete = Propositions.objects.get(id=_id_).complete
        answer = Propositions.objects.get(id=_id_).proposition

        result = str(int(complete)) + "/" + str(answer)

        return compatible_web_gl_response(result)

    else:
        return compatible_web_gl_response("error")


@csrf_exempt
def acceptation(request):
    """Called by second player: tell if he accepts the proposition from first player"""

    if "id" and "choice" in request.POST:
        choice = request.POST["choice"]  # "0" or "1"
        _id_ = request.POST['id']

        entry = Propositions.objects.get(id=_id_)
        entry.answer = choice  # Enter the choice of P2 in the table
        entry.save(force_update=True)

        return compatible_web_gl_response("ok")

    else:
        return compatible_web_gl_response("error")


@csrf_exempt
def accepted(request):
    """Called by first player: he asks if the second player accepted"""

    if 'id' in request.POST:
        _id_ = request.POST['id']

        answer = Propositions.objects.get(id=_id_).answer
        if answer is not None:
            result = "1/"+str(int(answer))
        else:
            result = "0/-1"  # Second player did not give his reply for the moment

        return compatible_web_gl_response(result)

    else:
        return compatible_web_gl_response("error")
