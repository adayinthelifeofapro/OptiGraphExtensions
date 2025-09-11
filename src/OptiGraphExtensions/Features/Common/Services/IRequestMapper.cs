namespace OptiGraphExtensions.Features.Common.Services;

public interface IRequestMapper<in TModel, out TCreateRequest, out TUpdateRequest>
    where TModel : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    TCreateRequest MapToCreateRequest(TModel model);
    TUpdateRequest MapToUpdateRequest(TModel model);
}